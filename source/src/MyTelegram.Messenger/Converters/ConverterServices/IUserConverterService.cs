namespace MyTelegram.Messenger.Converters.ConverterServices;

public interface IUserConverterService
{
    Task<ILayeredUser> GetUserAsync(long selfUserId, long userId, bool skipSetContactProperties = true,
        bool skipPrivacy = true, int layer = 0);

    Task<List<ILayeredUser>> GetUserListAsync(long selfUserId, List<long> userIds, bool skipSetContactProperties = true,
        bool skipCheckPrivacy = true, int layer = 0);

    Task<IUserFull> GetUserFullAsync(long selfUserId, long userId, int layer = 0);

    IUserFull ToUserFull(long selfUserId,
        IUserReadModel userReadModel,
        IReadOnlyCollection<IPhotoReadModel>? photoReadModels,
        IReadOnlyCollection<IContactReadModel>? contactReadModels,
        IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels, int layer = 0);
    ILayeredUser ToUser(long selfUserId, IUserReadModel userReadModel, IReadOnlyCollection<IPhotoReadModel>? photoReadModels = null,
        IContactReadModel? contactReadModel = null, IContactReadModel? targetUserContactReadModel = null, IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels = null, int layer = 0);

    List<ILayeredUser> ToUserList(long selfUserId, IReadOnlyCollection<IUserReadModel> userReadModels,
        IReadOnlyCollection<IPhotoReadModel> photoReadModels,
        IReadOnlyCollection<IContactReadModel> contactReadModels,
        IReadOnlyCollection<IPrivacyReadModel> privacyReadModels, int layer = 0);
}