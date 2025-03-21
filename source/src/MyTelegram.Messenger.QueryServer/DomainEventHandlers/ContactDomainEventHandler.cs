using MyTelegram.Messenger.Services.Interfaces;
using TPeerSettings = MyTelegram.Schema.TPeerSettings;
using TPhoto = MyTelegram.Schema.Photos.TPhoto;

namespace MyTelegram.Messenger.QueryServer.DomainEventHandlers;

public class ContactDomainEventHandler(
    IObjectMessageSender objectMessageSender,
    ICommandBus commandBus,
    IIdGenerator idGenerator,
    IAckCacheService ackCacheService,
    IQueryProcessor queryProcessor,
    IUserAppService userAppService,
    IUserConverterService userConverterService,
    //ILayeredService<IUserConverter> layeredUserService,
    IPhotoAppService photoAppService,
    //ILayeredService<IPhotoConverter> layeredPhotoService,
    ILayeredService<IPhotoConverter> photoLayeredService,
    IPrivacyAppService privacyAppService
    )
    : DomainEventHandlerBase(objectMessageSender,
            commandBus,
            idGenerator,
            ackCacheService),
        ISubscribeSynchronousTo<ContactAggregate, ContactId, ContactAddedEvent>,
        ISubscribeSynchronousTo<ContactAggregate, ContactId, ContactDeletedEvent>,
        ISubscribeSynchronousTo<ImportContactsSaga, ImportContactsSagaId, ImportContactsCompletedSagaEvent>,
        ISubscribeSynchronousTo<ContactAggregate, ContactId, ContactProfilePhotoChangedEvent>
{
    public async Task HandleAsync(IDomainEvent<ContactAggregate, ContactId, ContactAddedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        //var targetUser = await userAppService.GetAsync(domainEvent.AggregateEvent.TargetUserId);
        //var photos = await photoAppService.GetPhotosAsync(targetUser);
        //var privacyReadModels = await privacyAppService.GetPrivacyListAsync(domainEvent.AggregateEvent.TargetUserId);
        //var tUser = layeredUserService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
        //    .ToUser(domainEvent.AggregateEvent.SelfUserId, targetUser!, photos, null, privacyReadModels);
        var user = await userConverterService.GetUserAsync(domainEvent.AggregateEvent.SelfUserId,
            domainEvent.AggregateEvent.TargetUserId,
            true,
            false,
            domainEvent.AggregateEvent.RequestInfo.Layer
        );
        user.Contact = true;
        user.FirstName = domainEvent.AggregateEvent.FirstName;
        user.LastName = domainEvent.AggregateEvent.LastName;

        var r = new TUpdates
        {
            Chats = new TVector<IChat>(),
            Date = DateTime.UtcNow.ToTimestamp(),
            Seq = 0,
            Updates = new TVector<IUpdate>(new TUpdatePeerSettings
            {
                Peer = new TPeerUser { UserId = domainEvent.AggregateEvent.TargetUserId },
                Settings = new TPeerSettings { NeedContactsException = false }
            }),
            Users = new TVector<IUser>(user)
        };
        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, r);
    }

    public async Task HandleAsync(IDomainEvent<ContactAggregate, ContactId, ContactDeletedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        //var targetUser = await userAppService.GetAsync(domainEvent.AggregateEvent.TargetUid);
        //var photos = await photoAppService.GetPhotosAsync(targetUser);
        //var privacyList = await privacyAppService.GetPrivacyListAsync(domainEvent.AggregateEvent.TargetUid);
        //var tUser = layeredUserService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
        //    .ToUser(domainEvent.AggregateEvent.RequestInfo.UserId, targetUser!, photos, null, privacyList);

        var user = await userConverterService.GetUserAsync(domainEvent.AggregateEvent.RequestInfo.UserId,
            domainEvent.AggregateEvent.TargetUid,
            true, false,
            domainEvent.AggregateEvent.RequestInfo.Layer
        );

        var r = new TUpdates
        {
            Chats = new TVector<IChat>(),
            Date = DateTime.UtcNow.ToTimestamp(),
            Seq = 0,
            Updates = new TVector<IUpdate>(new TUpdatePeerSettings
            {
                Peer = new TPeerUser { UserId = domainEvent.AggregateEvent.TargetUid },
                Settings = new TPeerSettings()
            }),
            Users = new TVector<IUser>(user)
        };
        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, r);
    }

    public async Task HandleAsync(
        IDomainEvent<ContactAggregate, ContactId, ContactProfilePhotoChangedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        //var userReadModel = await userAppService.GetAsync(domainEvent.AggregateEvent.TargetUserId);
        //var privacyList = await privacyAppService.GetPrivacyListAsync(domainEvent.AggregateEvent.TargetUserId);
        //var photoReadModels = await photoAppService.GetPhotosAsync(userReadModel);
        //var photoReadModel = await photoAppService.GetAsync(domainEvent.AggregateEvent.PhotoId);
        //var contactReadModel =
        //    await queryProcessor.ProcessAsync(
        //        new GetContactQuery(domainEvent.AggregateEvent.RequestInfo.UserId,
        //            domainEvent.AggregateEvent.TargetUserId), cancellationToken);

        //var user = layeredUserService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
        //    .ToUser(domainEvent.AggregateEvent.SelfUserId, userReadModel!, photoReadModels, contactReadModel,
        //        privacyList);

        var user = await userConverterService.GetUserAsync(domainEvent.AggregateEvent.SelfUserId,
            domainEvent.AggregateEvent.TargetUserId,
            false,
            false,
            domainEvent.AggregateEvent.RequestInfo.Layer
        );
        var photoReadModel = await photoAppService.GetAsync(domainEvent.AggregateEvent.PhotoId);
        var photo = photoLayeredService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer)
            .ToPhoto(photoReadModel);
        var r = new TPhoto
        {
            Users = new TVector<IUser>(user),
            Photo = photo
        };

        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, r);
    }

    public async Task HandleAsync(
        IDomainEvent<ImportContactsSaga, ImportContactsSagaId, ImportContactsCompletedSagaEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        var importedContacts = domainEvent.AggregateEvent.PhoneContacts
            .Where(p => p.UserId > 0)
            .Select(p => new TImportedContact { ClientId = p.ClientId, UserId = p.UserId }).ToList();
        var userIds = importedContacts.Select(p => p.UserId).ToList();
        //var userReadModels = await userAppService.GetListAsync(userIds);
        //var photoReadModels = await photoAppService.GetPhotosAsync(userReadModels);
        //var privacyReadModels = await privacyAppService.GetPrivacyListAsync(userIds);
        //var contactReadModels =
        //    await queryProcessor.ProcessAsync(new GetContactListQuery(domainEvent.AggregateEvent.RequestInfo.UserId,
        //        userIds), cancellationToken);

        //var userList = layeredUserService.GetConverter(domainEvent.AggregateEvent.RequestInfo.Layer).ToUserList(
        //    domainEvent.AggregateEvent.RequestInfo.UserId, userReadModels, photoReadModels, contactReadModels,
        //    privacyReadModels);
        var users = await userConverterService.GetUserListAsync(domainEvent.AggregateEvent.RequestInfo.UserId,
            userIds, true, false, domainEvent.AggregateEvent.RequestInfo.Layer
        );

        foreach (var layeredUser in users)
        {
            layeredUser.Contact = true;
        }

        var r = new TImportedContacts
        {
            Imported = new TVector<IImportedContact>(importedContacts),
            PopularInvites = new TVector<IPopularContact>(),
            RetryContacts = new TVector<long>(),
            Users = new TVector<IUser>(users)
        };
        await SendRpcMessageToClientAsync(domainEvent.AggregateEvent.RequestInfo, r);

        var updates = new List<IUpdate>();
        foreach (var userId in userIds)
        {
            var updatePeerSettings = new TUpdatePeerSettings
            {
                Peer = new TPeerUser
                {
                    UserId = userId
                },
                Settings = new TPeerSettings()
            };
            updates.Add(updatePeerSettings);
        }

        await PushMessageToPeerAsync(domainEvent.AggregateEvent.RequestInfo.UserId.ToUserPeer(), new TUpdates
        {
            Updates = new TVector<IUpdate>(updates),
            Users = new TVector<IUser>(users),
            Chats = new TVector<IChat>(),
            Date = DateTime.Now.ToTimestamp()
        });
    }
}