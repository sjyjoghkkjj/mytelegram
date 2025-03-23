using MyTelegram.Messenger.Services.Impl;

namespace MyTelegram.Messenger.Converters.ConverterServices;

public class UserConverterService(
    IQueryProcessor queryProcessor,
    IUserAppService userAppService,
    IPhotoAppService photoAppService,
    IPrivacyAppService privacyAppService,
    IPrivacyHelper privacyHelper,
    IContactHelper contactHelper,
    //IBlockCacheAppService blockCacheAppService,
    IUserStatusCacheAppService userStatusCacheAppService,
    ILayeredService<IUserConverter> userLayeredService,
    ILayeredService<IUserFullConverter> userFullLayeredService,
    ILayeredService<IEmojiStatusConverter> emojiStatusLayeredService,
    ILayeredService<IPhotoConverter> photoLayeredService) : IUserConverterService, ITransientDependency
{
    public async Task<ILayeredUser> GetUserAsync(long selfUserId, long userId, bool skipSetContactProperties = true,
        bool skipCheckPrivacy = true, int layer = 0)
    {
        var userReadModel = await userAppService.GetAsync(userId);
        if (userReadModel == null)
        {
            throw new RpcException(RpcErrors.RpcErrors400.UserIdInvalid);
        }

        var photoReadModels = (await photoAppService.GetPhotosAsync(userReadModel)).ToDictionary(k => k.PhotoId);

        //IReadOnlyCollection<IContactReadModel>? contactReadModels = null;
        IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels = null;
        IContactReadModel? myContactReadModel = null;
        IContactReadModel? targetUserContactReadModel = null;
        if (!skipSetContactProperties)
        {
            var contactReadModels =
                await queryProcessor.ProcessAsync(new GetContactListBySelfIdAndTargetUserIdQuery(selfUserId, userId));
            myContactReadModel =
                contactReadModels?.FirstOrDefault(p => p.SelfUserId == selfUserId && p.TargetUserId == userId);
            targetUserContactReadModel =
                contactReadModels?.FirstOrDefault(p => p.SelfUserId == userId && p.TargetUserId == selfUserId);
        }

        if (!skipCheckPrivacy)
        {
            privacyReadModels = await privacyAppService.GetPrivacyListAsync(userId);
        }

        return ToUserCore(selfUserId, userReadModel, photoReadModels, myContactReadModel, targetUserContactReadModel,
            privacyReadModels, layer);
    }

    public async Task<List<ILayeredUser>> GetUserListAsync(long selfUserId, List<long> userIds,
        bool skipSetContactProperties = true,
        bool skipCheckPrivacy = true, int layer = 0)
    {
        var userReadModels = await userAppService.GetListAsync(userIds);
        var photoReadModels = await photoAppService.GetPhotosAsync(userReadModels);
        IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels = null;
        IReadOnlyCollection<IContactReadModel>? contactReadModels = null;
        if (!skipSetContactProperties)
        {
            contactReadModels = await queryProcessor.ProcessAsync(new GetContactListQuery(selfUserId, userIds));
        }

        if (!skipCheckPrivacy)
        {
            privacyReadModels = await privacyAppService.GetPrivacyListAsync(userIds);
        }

        return ToUserList(selfUserId, userReadModels, photoReadModels, contactReadModels, privacyReadModels, layer);
    }

    public IUserFull ToUserFull(long selfUserId,
        IUserReadModel userReadModel,
        IReadOnlyCollection<IPhotoReadModel>? photoReadModels,
        IReadOnlyCollection<IContactReadModel>? contactReadModels,
        IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels, int layer = 0)
    {
        var userId = userReadModel.UserId;
        var isOfficialUserId = userId == MyTelegramServerDomainConsts.OfficialUserId;
        var phoneCallAvailable = !isOfficialUserId &&
                                 !userReadModel.Bot &&
                                 userId != selfUserId;
        var userFull = userFullLayeredService.GetConverter(layer).ToUserFull(userReadModel);
        userFull.CanPinMessage = !isOfficialUserId;
        userFull.PhoneCallsAvailable = phoneCallAvailable;
        userFull.VideoCallsAvailable = phoneCallAvailable;
        userFull.PhoneCallsPrivate = isOfficialUserId;
        //userFull.Blocked = isBlocked;
        //userFull.NotifySettings

        var photos = photoReadModels?.ToDictionary(k => k.PhotoId) ?? [];

        if (userReadModel.ProfilePhotoId != null)
        {
            if (photos.TryGetValue(userReadModel.ProfilePhotoId.Value, out var profilePhotoReadModel))
            {
                userFull.ProfilePhoto = photoLayeredService.GetConverter(layer).ToPhoto(profilePhotoReadModel);
            }
        }

        if (userReadModel.FallbackPhotoId != null)
        {
            if (photos.TryGetValue(userReadModel.FallbackPhotoId.Value, out var fallbackPhotoReadModel))
            {
                userFull.FallbackPhoto = photoLayeredService.GetConverter(layer).ToPhoto(fallbackPhotoReadModel);
            }
        }

        if (selfUserId != userId)
        {
            var myContactReadModel =
                contactReadModels?.FirstOrDefault(p => p.SelfUserId == selfUserId && p.TargetUserId == userId);
            var targetUserContactReadModel =
                contactReadModels?.FirstOrDefault(p => p.SelfUserId == userId && p.TargetUserId == selfUserId);
            var contactType = contactHelper.GetContactType(myContactReadModel, targetUserContactReadModel);

            ApplyPrivacyToUserFull(selfUserId, userFull, privacyReadModels, contactType);
        }

        return userFull;
    }

    public async Task<IUserFull> GetUserFullAsync(long selfUserId, long userId, int layer = 0)
    {
        var userReadModel = await userAppService.GetAsync(userId);
        //var isBlocked = await blockCacheAppService.IsBlockedAsync(selfUserId, userId);
        var photoReadModels = await photoAppService.GetPhotosAsync(userReadModel);
        var privacyReadModels = await privacyAppService.GetPrivacyListAsync(userId);
        IReadOnlyCollection<IContactReadModel>? contactReadModels = null;
        if (selfUserId != userId)
        {
            contactReadModels =
              await queryProcessor.ProcessAsync(new GetContactListBySelfIdAndTargetUserIdQuery(selfUserId, userId));
        }

        return ToUserFull(selfUserId, userReadModel, photoReadModels, contactReadModels, privacyReadModels, layer);
    }

    public ILayeredUser ToUser(long selfUserId, IUserReadModel userReadModel, IReadOnlyCollection<IPhotoReadModel>? photoReadModels = null,
        IContactReadModel? contactReadModel = null, IContactReadModel? targetUserContactReadModel = null, IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels = null, int layer = 0)
    {
        var photos = photoReadModels?.ToDictionary(k => k.PhotoId);

        return ToUserCore(selfUserId, userReadModel, photos, contactReadModel, targetUserContactReadModel,
            privacyReadModels, layer);
    }

    public List<ILayeredUser> ToUserList(long selfUserId, IReadOnlyCollection<IUserReadModel> userReadModels, IReadOnlyCollection<IPhotoReadModel>? photoReadModels = null,
        IReadOnlyCollection<IContactReadModel>? contactReadModels = null, IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels = null, int layer = 0)
    {
        var users = new List<ILayeredUser>();

        var photos = photoReadModels?
            .DistinctBy(p => p.PhotoId)
            .ToDictionary(k => k.PhotoId) ?? [];

        var targetUserContacts = contactReadModels?
             .DistinctBy(p => p.SelfUserId)
             .ToDictionary(k => k.SelfUserId) ?? [];

        var myContacts = contactReadModels?
             .Where(p => p.SelfUserId == selfUserId)
             .DistinctBy(p => p.TargetUserId)
             .ToDictionary(k => k.TargetUserId) ?? [];

        var groupedPrivacyReadModels = privacyReadModels?.GroupBy(p => p.UserId).ToDictionary(k => k.Key, v => v.ToList()) ?? [];

        foreach (var userReadModel in userReadModels)
        {
            myContacts.TryGetValue(userReadModel.UserId, out var myContactReadModel);
            targetUserContacts.TryGetValue(selfUserId, out var targetUserContactReadModel);
            groupedPrivacyReadModels.TryGetValue(userReadModel.UserId, out var currentUserPrivacyReadModels);
            var user = ToUserCore(selfUserId, userReadModel, photos, myContactReadModel,
                targetUserContactReadModel, currentUserPrivacyReadModels, layer);
            users.Add(user);
        }

        return users;
    }

    private ILayeredUser ToUserCore(long selfUserId, IUserReadModel userReadModel,
        Dictionary<long, IPhotoReadModel>? photoReadModels = null,
        //Dictionary<long, IContactReadModel>? contactReadModels = null,
        IContactReadModel? myContactReadModel = null,
        IContactReadModel? targetUserContactReadModel = null,
        IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels = null, int layer = 0)
    {
        var user = userLayeredService.GetConverter(layer).ToUser(userReadModel);
        if (selfUserId == userReadModel.UserId)
        {
            user.Self = true;
        }

        user.Status = userStatusCacheAppService.GetUserStatus(user.Id);
        EmojiStatus? emojiStatus = null;
        if (userReadModel.EmojiStatusDocumentId != null)
        {
            emojiStatus = new EmojiStatus(userReadModel.EmojiStatusDocumentId.Value, userReadModel.EmojiStatusValidUntil);
        }

        user.EmojiStatus = emojiStatusLayeredService.GetConverter(layer).ToEmojiStatus(emojiStatus);
        //var contactReadModel =
        //    contactReadModels?.FirstOrDefault(p =>
        //        p.SelfUserId == selfUserId && p.TargetUserId == userReadModel.UserId);
        var contactType = contactHelper.GetContactType(myContactReadModel, targetUserContactReadModel);
        var photos = photoReadModels ?? [];
        SetUserProfilePhoto(userReadModel, user, photos, layer);
        SetContactPersonalProfilePhoto(user, photos, myContactReadModel, layer);
        ApplyPrivacyToUser(selfUserId, userReadModel, user, photos, contactType, privacyReadModels, layer);

        if (!user.Self)
        {
            switch (contactType)
            {
                case ContactType.None:
                case ContactType.TargetUserIsMyContact:
                    user.Phone = null;
                    break;
                case ContactType.ContactOfTargetUser:
                case ContactType.Mutual:
                    break;
            }
        }

        return user;
    }

    private void ApplyPrivacyToUserFull(long selfUserId,
        IUserFull userFull,
        IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels,
        ContactType contactType)
    {
        if (selfUserId == userFull.Id)
        {
            return;
        }

        foreach (var privacy in privacyReadModels ?? [])
        {
            switch (privacy.PrivacyType)
            {
                case PrivacyType.PhoneCall:
                    privacyHelper.ApplyPrivacy(privacy, _ =>
                    {
                        userFull.PhoneCallsAvailable = false;
                        userFull.PhoneCallsPrivate = false;
                    }, selfUserId, contactType);
                    break;

                case PrivacyType.ProfilePhoto:
                    privacyHelper.ApplyPrivacy(privacy,
                        _ =>
                        {
                            userFull.ProfilePhoto = null;
                        },
                        selfUserId, contactType);
                    break;

                case PrivacyType.VoiceMessages:
                    privacyHelper.ApplyPrivacy(privacy, _ => { userFull.VoiceMessagesForbidden = true; },
                        selfUserId, contactType);
                    break;
                case PrivacyType.About:
                    privacyHelper.ApplyPrivacy(privacy, _ => { userFull.About = null; }, selfUserId, contactType);
                    break;

                case PrivacyType.Birthday:
                    privacyHelper.ApplyPrivacy(privacy, _ =>
                    {
                        userFull.Birthday = null;
                    }, selfUserId, contactType);
                    break;
            }
        }
    }


    private void ApplyPrivacyToUser(long selfUserId, IUserReadModel userReadModel, ILayeredUser user,
        Dictionary<long, IPhotoReadModel> photos, ContactType contactType,
        IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels, int layer)
    {
        //var privacyReadModels = await privacyAppService.GetPrivacyListAsync(userReadModel.UserId);
        if (selfUserId != userReadModel.UserId && privacyReadModels?.Count > 0)
        {
            photos.TryGetValue(userReadModel.FallbackPhotoId ?? 0, out var fallbackPhotoReadModel);
            //var contactType = contactHelper.GetContactType(selfUserId, userReadModel.UserId, contactReadModels);

            foreach (var privacy in privacyReadModels)
            {
                switch (privacy.PrivacyType)
                {
                    case PrivacyType.StatusTimestamp:
                        privacyHelper.ApplyPrivacy(privacy,
                            _ =>
                            {
                                switch (user.Status)
                                {
                                    case TUserStatusOnline:
                                        break;
                                    case TUserStatusRecently:
                                        break;
                                    case TUserStatusOffline:
                                        user.Status = new TUserStatusRecently();
                                        break;
                                    default:
                                        user.Status = new TUserStatusEmpty();
                                        break;
                                }
                            },
                            selfUserId,
                            contactType);
                        break;
                    case PrivacyType.ProfilePhoto:
                        privacyHelper.ApplyPrivacy(privacy,
                            _ => user.Photo = photoLayeredService.GetConverter(layer)
                                .ToProfilePhoto(fallbackPhotoReadModel), selfUserId,
                            contactType);
                        break;
                    case PrivacyType.PhoneNumber:
                        privacyHelper.ApplyPrivacy(privacy, _ => user.Phone = null, selfUserId, contactType);
                        break;
                }
            }
        }
    }

    private void SetContactPersonalProfilePhoto(ILayeredUser user, Dictionary<long, IPhotoReadModel> photos,
        IContactReadModel? contactReadModel,
        int layer)
    {
        if (contactReadModel != null)
        {
            user.Contact = true;
            user.FirstName = contactReadModel.FirstName;
            user.LastName = contactReadModel.LastName;

            if (contactReadModel.PhotoId != null)
            {
                if (photos.TryGetValue(contactReadModel.PhotoId.Value, out var photoReadModel))
                {
                    user.Photo = photoLayeredService.GetConverter(layer).ToProfilePhoto(photoReadModel);
                    if (user.Photo is TUserProfilePhoto profilePhoto)
                    {
                        profilePhoto.Personal = true;
                    }
                }
            }
        }
    }

    private void SetUserProfilePhoto(IUserReadModel userReadModel,
        ILayeredUser user, Dictionary<long, IPhotoReadModel> photoReadModels, int layer)
    {
        if (userReadModel.ProfilePhotoId != null)
        {
            if (photoReadModels.TryGetValue(userReadModel.ProfilePhotoId.Value, out var photoReadModel))
            {
                user.Photo = photoLayeredService.GetConverter(layer).ToProfilePhoto(photoReadModel);
            }
        }
    }
}