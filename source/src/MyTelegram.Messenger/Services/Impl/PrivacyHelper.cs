namespace MyTelegram.Messenger.Services.Impl;

public class PrivacyHelper : IPrivacyHelper, ITransientDependency
{
    public void ApplyPrivacy(IPrivacyReadModel? privacyReadModel,
        Action executeOnPrivacyNotMatch,
        long selfUserId,
        bool isContact)
    {
        ApplyPrivacy(privacyReadModel, _ => executeOnPrivacyNotMatch(), selfUserId, isContact ? ContactType.Mutual : ContactType.None);
    }

    public void ApplyPrivacy(IPrivacyReadModel? privacyReadModel, Action<PrivacyValueType> executeOnPrivacyNotMatch, long selfUserId,
        ContactType contactType)
    {
        if (privacyReadModel == null)
        {
            return;
        }

        var allowed = IsAllowedByPrivacy(selfUserId, privacyReadModel, contactType);
        if (!allowed)
        {
            var rule = privacyReadModel.PrivacyValueDataList.FirstOrDefault();
            executeOnPrivacyNotMatch(rule?.PrivacyValueType ?? PrivacyValueType.DisallowAll);
        }
    }

    public bool IsAllowedByPrivacy(long selfUserId, IPrivacyReadModel? privacyReadModel, ContactType contactType)
    {
        if (privacyReadModel == null)
        {
            return true;
        }

        // Простая модель: AllowAll > DisallowAll; Contacts учитывает ContactType
        var hasAllowAll = privacyReadModel.PrivacyValueDataList.Any(p => p.PrivacyValueType == PrivacyValueType.AllowAll);
        if (hasAllowAll)
        {
            return true;
        }

        var hasDisallowAll = privacyReadModel.PrivacyValueDataList.Any(p => p.PrivacyValueType == PrivacyValueType.DisallowAll);
        if (hasDisallowAll)
        {
            return false;
        }

        var allowContacts = privacyReadModel.PrivacyValueDataList.Any(p => p.PrivacyValueType == PrivacyValueType.AllowContacts);
        if (allowContacts && contactType is ContactType.Mutual or ContactType.ContactOfTargetUser or ContactType.TargetUserIsMyContact)
        {
            return true;
        }

        var disallowContacts = privacyReadModel.PrivacyValueDataList.Any(p => p.PrivacyValueType == PrivacyValueType.DisallowContacts);
        if (disallowContacts && contactType is ContactType.Mutual or ContactType.ContactOfTargetUser or ContactType.TargetUserIsMyContact)
        {
            return false;
        }

        return true;
    }

    public bool CanSee(PrivacyType privacyType, long selfUserId, long targetUserId, IReadOnlyCollection<IPrivacyReadModel>? privacyReadModels)
    {
        if (privacyReadModels == null)
        {
            return true;
        }
        var model = privacyReadModels.FirstOrDefault(p => p.PrivacyType == privacyType);
        // TODO: вычисление ContactType по self/target; упрощённо считаем None
        return IsAllowedByPrivacy(selfUserId, model, ContactType.None);
    }
}