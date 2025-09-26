namespace MyTelegram.Messenger.Handlers.LatestLayer.Messages;

///<summary>
/// React to message.Starting from layer 159, the reaction will be sent from the peer specified using <a href="https://corefork.telegram.org/method/messages.saveDefaultSendAs">messages.saveDefaultSendAs</a>.
/// <para>Possible errors</para>
/// Code Type Description
/// 400 CHANNEL_INVALID The provided channel is invalid.
/// 400 CHANNEL_PRIVATE You haven't joined this channel/supergroup.
/// 403 CHAT_WRITE_FORBIDDEN You can't write in this chat.
/// 400 MESSAGE_ID_INVALID The provided message id is invalid.
/// 400 MESSAGE_NOT_MODIFIED The provided message data is identical to the previous message data, the message wasn't modified.
/// 400 MSG_ID_INVALID Invalid message ID provided.
/// 400 PEER_ID_INVALID The provided peer id is invalid.
/// 403 PREMIUM_ACCOUNT_REQUIRED A premium account is required to execute this action.
/// 400 REACTIONS_TOO_MANY The message already has exactly <code>reactions_uniq_max</code> reaction emojis, you can't react with a new emoji, see <a href="https://corefork.telegram.org/api/config#client-configuration">the docs for more info »</a>.
/// 400 REACTION_EMPTY Empty reaction provided.
/// 400 REACTION_INVALID The specified reaction is invalid.
/// 400 USER_BANNED_IN_CHANNEL You're banned from sending messages in supergroups/channels.
/// See <a href="https://corefork.telegram.org/method/messages.sendReaction" />
///</summary>
internal sealed class SendReactionHandler(
    ICommandBus commandBus,
    IPeerHelper peerHelper,
    IAccessHashHelper accessHashHelper,
    IQueryProcessor queryProcessor,
    IAppConfigHelper appConfigHelper,
    IUpdatesConverterService updatesConverterService,
    IMessageConverterService messageConverterService,
    IChannelAppService channelAppService,
    IUserAppService userAppService)
    : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestSendReaction, MyTelegram.Schema.IUpdates>
{
    protected override async Task<MyTelegram.Schema.IUpdates> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestSendReaction obj)
    {
        await accessHashHelper.CheckAccessHashAsync(input, obj.Peer);

        var toPeer = peerHelper.GetPeer(obj.Peer, input.UserId);
        var ownerPeerId = toPeer.PeerType == PeerType.Channel ? toPeer.PeerId : input.UserId;
        var aggregateId = MessageId.Create(ownerPeerId, obj.MsgId);

        // Rights & limits
        if (toPeer.PeerType == PeerType.Channel)
        {
            var channel = await channelAppService.GetAsync(toPeer.PeerId);
            var member = channel.AdminList.FirstOrDefault(a => a.UserId == input.UserId);
            if (channel.DefaultBannedRights?.SendReactions ?? false)
            {
                RpcErrors.RpcErrors403.ChatWriteForbidden.ThrowRpcError();
            }
            if (member != null && member.AdminRights.Anonymous)
            {
                RpcErrors.RpcErrors403.AnonymousReactionsDisabled.ThrowRpcError();
            }
        }
        var requester = await userAppService.GetAsync(input.UserId);
        var isPremium = requester.Premium;
        var reactionsUserMax = isPremium ? 3 : 1;
        var appCfg = appConfigHelper.GetAppConfig();
        if (appCfg is TJsonObject json2)
        {
            var prem = json2.Value.FirstOrDefault(x => x.Key == "reactions_user_max_premium")?.Value as TJsonNumber;
            var def = json2.Value.FirstOrDefault(x => x.Key == "reactions_user_max_default")?.Value as TJsonNumber;
            if (prem != null && isPremium) reactionsUserMax = (int)prem.Value;
            if (def != null && !isPremium) reactionsUserMax = (int)def.Value;
        }

        // Toggle semantics: if no reactions specified -> clear all
        var reactions = obj.Reaction?.ToList() ?? [];
        if (reactions.Count == 0)
        {
            // Fetch current user reactions and remove them
            var current = await queryProcessor.ProcessAsync(new GetMessageReactionsListQuery(input.UserId, toPeer, obj.MsgId, null, 0, 1000));
            foreach (var r in current.Where(r => r.UserId == input.UserId))
            {
                var remove = new RemoveReactionCommand(aggregateId, input.ToRequestInfo(), input.UserId, r.Reaction.ToSchema());
                await commandBus.PublishAsync(remove);
            }
        }
        else
        {
            // Enforce limits and toggle for same reaction
            var current = await queryProcessor.ProcessAsync(new GetMessageReactionsListQuery(input.UserId, toPeer, obj.MsgId, null, 0, 100));
            var my = current.Where(x => x.UserId == input.UserId).ToList();

            // Unique reaction limit on message
            var appConfig = appConfigHelper.GetAppConfig();
            var uniqMax = 11; // default fallback
            if (appConfig is TJsonObject json)
            {
                var v = json.Value.FirstOrDefault(x => x.Key == "reactions_uniq_max")?.Value as TJsonNumber;
                if (v != null) { uniqMax = (int)v.Value; }
            }
            if (toPeer.PeerType == PeerType.Channel)
            {
                var channelFull = await queryProcessor.ProcessAsync(new GetChannelFullByIdQuery(toPeer.PeerId));
                if (channelFull?.ReactionsLimit.HasValue ?? false)
                {
                    uniqMax = channelFull.ReactionsLimit.Value;
                }
                if (channelFull?.AvailableReactions != null && channelFull.AvailableReactions.Count > 0)
                {
                    // allow only configured reactions in this chat
                    foreach (var r in reactions)
                    {
                        if (r is TReactionEmoji re && !channelFull.AvailableReactions.Contains(re.Emoticon))
                        {
                            RpcErrors.RpcErrors400.ReactionInvalid.ThrowRpcError();
                        }
                    }
                }
            }

            // If sending the same reaction already present -> remove instead
            foreach (var r in reactions)
            {
                var reactionId = r.GetReactionId();
                if (my.Any(m => m.ReactionId == reactionId))
                {
                    var removeCmd = new RemoveReactionCommand(aggregateId, input.ToRequestInfo(), input.UserId, r);
                    await commandBus.PublishAsync(removeCmd);
                }
                else
                {
                    if (my.Count >= reactionsUserMax)
                    {
                        RpcErrors.RpcErrors400.ReactionsTooMany.ThrowRpcError();
                    }
                    // check uniqMax: count unique reactions on message
                    var all = await queryProcessor.ProcessAsync(new GetMessageReactionsListQuery(input.UserId, toPeer, obj.MsgId, null, 0, 1000));
                    var currentUnique = all.Select(a => a.ReactionId).Distinct().Count();
                    var newUnique = currentUnique;
                    if (!all.Any(a => a.ReactionId == reactionId))
                    {
                        newUnique++;
                    }
                    if (newUnique > uniqMax)
                    {
                        RpcErrors.RpcErrors400.ReactionsTooMany.ThrowRpcError();
                    }
                    var sendCmd = new SendReactionCommand(aggregateId, input.ToRequestInfo(), input.UserId, r, obj.AddToRecent);
                    await commandBus.PublishAsync(sendCmd);
                }
            }
        }

        // Build updateMessageReactions response from read model
        var message = await queryProcessor.ProcessAsync(new GetMessageByPeerIdAndMessageIdQuery(ownerPeerId, obj.MsgId));
        if (message == null)
        {
            return new TUpdates { Updates = [], Chats = [], Users = [], Date = CurrentDate };
        }

        var userReactions = await queryProcessor.ProcessAsync(new GetMessageReactionsListQuery(input.UserId, toPeer, obj.MsgId, null, 0, 1000));
        var counts = new Dictionary<long, (IReaction reaction, int count)>();
        foreach (var ur in userReactions)
        {
            var id = ur.ReactionId;
            if (!counts.ContainsKey(id))
            {
                counts[id] = (ur.Reaction.ToSchema(), 0);
            }
            counts[id] = (counts[id].reaction, counts[id].count + 1);
        }

        var results = new TVector<IReactionCount>();
        foreach (var kv in counts)
        {
            results.Add(new TReactionCount { Reaction = kv.Value.reaction, Count = kv.Value.count });
        }

        var recent = new TVector<IMessagePeerReaction>();
        foreach (var ur in userReactions.OrderByDescending(x => x.Reaction.Date ?? 0).Take(10))
        {
            recent.Add(new TMessagePeerReaction
            {
                My = ur.UserId == input.UserId,
                Big = false,
                Date = ur.Reaction.Date ?? CurrentDate,
                Peer = ur.UserId.ToUserPeer(),
                Reaction = ur.Reaction.ToSchema()
            });
        }

        var msgReactions = new TMessageReactions
        {
            Results = results,
            RecentReactions = recent,
            Min = false,
            CanSeeList = true
        };

        var update = new TUpdateMessageReactions
        {
            Peer = toPeer.ToPeer(),
            MsgId = obj.MsgId,
            Reactions = msgReactions
        };

        return new TUpdates
        {
            Updates = new TVector<IUpdate>(update, new TUpdateRecentReactions()),
            Chats = [],
            Users = [],
            Date = CurrentDate
        };
    }
}
