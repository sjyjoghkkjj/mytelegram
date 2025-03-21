namespace MyTelegram.Messenger.Services.Impl;

public interface IContactHelper
{
    ContactType GetContactType(IContactReadModel? myContactReadModel,
        IContactReadModel? targetUserContactReadModel);
    ContactType GetContactType(long selfUserId, long targetUserId,
        IReadOnlyCollection<IContactReadModel> contactReadModels);

    //Task<ContactType> GetContactTypeAsync(long selfUserId, long targetUserId);
}

public class ContactHelper : IContactHelper, ITransientDependency
{
    public ContactType GetContactType(IContactReadModel? myContactReadModel,
        IContactReadModel? targetUserContactReadModel)
    {
        var contactType = (myContactReadModel, targetUserContactReadModel)
            switch
            {
                { myContactReadModel: not null, targetUserContactReadModel: not null } => ContactType.Mutual,
                { myContactReadModel: null, targetUserContactReadModel: not null } => ContactType
                    .ContactOfTargetUser,
                { myContactReadModel: not null, targetUserContactReadModel: null } => ContactType
                    .TargetUserIsMyContact,
                _ => ContactType.None
            };

        return contactType;
    }

    public ContactType GetContactType(long selfUserId, long targetUserId, IReadOnlyCollection<IContactReadModel> contactReadModels)
    {
        var myContactReadModel =
            contactReadModels.FirstOrDefault(p => p.SelfUserId == selfUserId && p.TargetUserId == targetUserId);
        var targetUserContactReadModel =
            contactReadModels.FirstOrDefault(p => p.SelfUserId == targetUserId && p.TargetUserId == selfUserId);

        var contactType = (myContactReadModel, targetUserContactReadModel)
            switch
            {
                { myContactReadModel: not null, targetUserContactReadModel: not null } => ContactType.Mutual,
                { myContactReadModel: null, targetUserContactReadModel: not null } => ContactType
                    .ContactOfTargetUser,
                { myContactReadModel: not null, targetUserContactReadModel: null } => ContactType
                    .TargetUserIsMyContact,
                _ => ContactType.None
            };

        return contactType;
    }
}

public class ContactAppService(
    IQueryProcessor queryProcessor,
    IPhotoAppService photoAppService,
    IChannelAppService channelAppService,
    IUserAppService userAppService,
    IPeerHelper peerHelper,
    IOptionsMonitor<MyTelegramMessengerServerOptions> options)
    : BaseAppService, IContactAppService, ITransientDependency
{
    public ContactType GetContactType(long selfUserId, long targetUserId,
        IReadOnlyCollection<IContactReadModel> contactReadModels)
    {
        var myContactReadModel =
            contactReadModels.FirstOrDefault(p => p.SelfUserId == selfUserId && p.TargetUserId == targetUserId);
        var targetUserContactReadModel =
            contactReadModels.FirstOrDefault(p => p.SelfUserId == targetUserId && p.TargetUserId == selfUserId);

        var contactType = (myContactReadModel, targetUserContactReadModel)
            switch
        {
            { myContactReadModel: not null, targetUserContactReadModel: not null } => ContactType.Mutual,
            { myContactReadModel: null, targetUserContactReadModel: not null } => ContactType
                .ContactOfTargetUser,
            { myContactReadModel: not null, targetUserContactReadModel: null } => ContactType
                .TargetUserIsMyContact,
            _ => ContactType.None
        };

        return contactType;
    }

    public async Task<ContactType> GetContactTypeAsync(long selfUserId, long targetUserId)
    {
        var contactReadModels =
            await queryProcessor.ProcessAsync(new GetContactListBySelfIdAndTargetUserIdQuery(selfUserId, targetUserId));

        return GetContactType(selfUserId, targetUserId, contactReadModels);
    }

    public async Task<SearchContactOutput> SearchAsync(long selfUserId,
            string keyword)
    {
        if (keyword?.Length > 0)
        {
            var searchKeyword = keyword;
            if (searchKeyword.StartsWith("@"))
            {
                searchKeyword = keyword[1..];
            }

            var contactReadModels = await queryProcessor
                .ProcessAsync(new SearchContactQuery(selfUserId, searchKeyword));
            var userNameReadModels = await queryProcessor
                .ProcessAsync(new SearchUserNameQuery(searchKeyword));

            var channelIdList = userNameReadModels.Where(p => p.PeerType == PeerType.Channel).Select(p => p.PeerId)
                .ToList();

            var userIdList = contactReadModels.Select(p => p.TargetUserId).ToList();
            userIdList.AddRange(userNameReadModels.Where(p => p.PeerType == PeerType.User).Select(p => p.PeerId));

            var collectibleUsernameReadModel =
                await queryProcessor.ProcessAsync(new GetCollectibleUsernameByUsernameQuery(searchKeyword));
            if (collectibleUsernameReadModel != null)
            {
                var peerType = peerHelper.GetPeerType(collectibleUsernameReadModel.OwnerPeerId);
                switch (peerType)
                {
                    case PeerType.User:
                        userIdList.Add(collectibleUsernameReadModel.OwnerPeerId);
                        break;

                    case PeerType.Channel:
                        channelIdList.Add(collectibleUsernameReadModel.OwnerPeerId);
                        break;
                }
            }

            var userReadModels = await userAppService.GetListAsync(userIdList);
            var allUserReadModels = userReadModels.ToList();

            if (options.CurrentValue.EnableSearchNonContacts)
            {
                var userReadModels2 = await queryProcessor.ProcessAsync(new SearchUserByKeywordQuery(keyword, 20));
                allUserReadModels.AddRange(userReadModels2);
            }

            var channelReadModels = await channelAppService.GetListAsync(channelIdList);
            var photos = await photoAppService.GetPhotosAsync(allUserReadModels, contactReadModels);

            var privacyReadModels = await queryProcessor.ProcessAsync(new GetPrivacyListQuery(allUserReadModels.Select(p => p.UserId).ToList(), new List<PrivacyType>
            {
                PrivacyType.PhoneNumber,
                PrivacyType.PhoneCall,
                PrivacyType.VoiceMessages,
                PrivacyType.ProfilePhoto,
                PrivacyType.StatusTimestamp,
                PrivacyType.Birthday,
                PrivacyType.About
            }));

            return new SearchContactOutput(selfUserId,
                allUserReadModels,
                photos,
                contactReadModels,
                [],
                channelReadModels,
                privacyReadModels,
                []
            );
        }

        return new SearchContactOutput(selfUserId,
            new List<IUserReadModel>(),
            new List<IPhotoReadModel>(),
            new List<IContactReadModel>(),
            new List<IChannelReadModel>(),
            new List<IChannelReadModel>(),
            new List<IPrivacyReadModel>(),
            new List<IChannelMemberReadModel>());
    }
}