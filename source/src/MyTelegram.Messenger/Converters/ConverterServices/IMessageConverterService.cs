namespace MyTelegram.Messenger.Converters.ConverterServices;

public interface IMessageConverterService
{
    IMessage ToMessage(long selfUserId, IMessageReadModel readModel,
        IPollReadModel? pollReadModel = null,
        List<string>? chosenOptions = null,
        int layer = 0);

    List<IMessage> ToMessageList(long selfUserId,
        IReadOnlyCollection<IMessageReadModel> messageReadModels,
        IReadOnlyCollection<IPollReadModel>? pollReadModels,
        IReadOnlyCollection<IPollAnswerVoterReadModel>? pollAnswerVoterReadModels,
        int layer = 0
    );

    IMessage ToMessage(long selfUserId,
        MessageItem item,
        List<ReactionCount>? reactions = null,
        List<Reaction>? recentReactions = null,
        List<UserReaction>? userReactions = null,
        bool mentioned = false,
        int layer = 0
    );

    //IMessages ToMessages(GetMessageOutput output, int layer = 0);
}