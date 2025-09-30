namespace MyTelegram.Messenger.Services.Impl;

public class PrivacyAppService(
    ICacheManager<GlobalPrivacySettingsCacheItem> cacheManager,
    IQueryProcessor queryProcessor,
    ICommandBus commandBus)
    : BaseAppService, IPrivacyAppService, ITransientDependency
{
    public async Task<IReadOnlyCollection<IPrivacyReadModel>> GetPrivacyListAsync(IReadOnlyList<long> userIds)
    {
        var list = await queryProcessor.ProcessAsync(new GetPrivacyListQuery(userIds, [PrivacyType.StatusTimestamp, PrivacyType.PhoneNumber, PrivacyType.ProfilePhoto]));
        return list;
    }

    public Task<IReadOnlyCollection<IPrivacyReadModel>> GetPrivacyListAsync(long userId)
    {
        return GetPrivacyListAsync([userId]);
    }

    public Task ApplyPrivacyAsync(long selfUserId, long targetUserId, Action executeOnPrivacyNotMatch, List<PrivacyType> privacyTypes)
    {
        return Task.CompletedTask;
    }

    public Task ApplyPrivacyListAsync(long selfUserId, IReadOnlyList<long> targetUserIdList, Action<long> executeOnPrivacyNotMatch,
        List<PrivacyType> privacyTypes)
    {
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<IPrivacyRule>> GetPrivacyRulesAsync(long selfUserId,
        IInputPrivacyKey key)
    {
        var type = MapKey(key);
        var list = await queryProcessor.ProcessAsync(new GetPrivacyListQuery([selfUserId], [type]));
        var rules = list.FirstOrDefault()?.PrivacyValueDataList?.Select(ToRule).OfType<IPrivacyRule>().ToList() ?? [];
        return rules;
    }

    public Task ApplyPrivacyListAsync(long selfUserId, IReadOnlyList<long> targetUserIdList, Action<PrivacyValueType, long> executeOnPrivacyNotMatch,
        List<PrivacyType> privacyTypes)
    {
        return Task.CompletedTask;
    }

    public Task SetGlobalPrivacySettingsAsync(long selfUserId, GlobalPrivacySettings globalPrivacySettings)
    {
        return Task.CompletedTask;
    }

    public async Task<GlobalPrivacySettingsCacheItem?> GetGlobalPrivacySettingsAsync(long userId)
    {
        var cacheKey = GlobalPrivacySettingsCacheItem.GetCacheKey(userId);
        var item = await cacheManager.GetAsync(cacheKey);
        var globalPrivacySettings = await queryProcessor.ProcessAsync(new GetGlobalPrivacySettingsQuery(userId));
        if (globalPrivacySettings != null)
        {
            item = new GlobalPrivacySettingsCacheItem(globalPrivacySettings.ArchiveAndMuteNewNoncontactPeers,
                globalPrivacySettings.KeepArchivedUnmuted, globalPrivacySettings.KeepArchivedFolders,
                globalPrivacySettings.HideReadMarks, globalPrivacySettings.NewNoncontactPeersRequirePremium);
            await cacheManager.SetAsync(cacheKey, item);
        }
        return item;
    }

    public PrivacyValueData GetPrivacyValueData(IInputPrivacyRule rule)
    {
        return rule switch
        {
            TPrivacyValueAllowAll => new PrivacyValueData(PrivacyValueType.AllowAll),
            TPrivacyValueDisallowAll => new PrivacyValueData(PrivacyValueType.DisallowAll),
            TPrivacyValueAllowContacts => new PrivacyValueData(PrivacyValueType.AllowContacts),
            TPrivacyValueDisallowContacts => new PrivacyValueData(PrivacyValueType.DisallowContacts),
            TPrivacyValueAllowUsers a => new PrivacyValueData(PrivacyValueType.AllowUsers, JsonSerializer.Serialize(a.Users.ToList())),
            TPrivacyValueDisallowUsers d => new PrivacyValueData(PrivacyValueType.DisallowUsers, JsonSerializer.Serialize(d.Users.ToList())),
            _ => new PrivacyValueData(PrivacyValueType.AllowAll)
        };
    }

    public List<PrivacyValueData> GetPrivacyValueDataList(IList<IInputPrivacyRule> rules)
    {
        return rules.Select(GetPrivacyValueData).ToList();
    }

    public async Task<SetPrivacyOutput> SetPrivacyAsync(RequestInfo requestInfo,
        long selfUserId,
        IInputPrivacyKey key,
        IReadOnlyList<IInputPrivacyRule> ruleList)
    {
        var type = MapKey(key);
        var data = GetPrivacyValueDataList(ruleList.ToList());
        var cmd = new UpdatePrivacyCommand(PrivacyId.Create(selfUserId, type), requestInfo, selfUserId, type, data);
        await commandBus.PublishAsync(cmd);
        var rules = ruleList.Select(r => (IPrivacyRule)r).ToList();
        return new SetPrivacyOutput(rules);
    }

    public Task ApplyPrivacyAsync(long selfUserId, long targetUserId, Action<PrivacyValueType> executeOnPrivacyNotMatch, PrivacyType privacyType)
    {
        return Task.CompletedTask;
    }

    public Task ApplyPrivacyAsync(long selfUserId, long targetUserId, Action<PrivacyValueType> executeOnPrivacyNotMatch, List<PrivacyType> privacyTypes)
    {
        return Task.CompletedTask;
    }
}

file scoped helper
partial class PrivacyAppService
{
    private static IPrivacyRule? ToRule(PrivacyValueData data)
    {
        return data.PrivacyValueType switch
        {
            PrivacyValueType.AllowAll => new TPrivacyValueAllowAll(),
            PrivacyValueType.DisallowAll => new TPrivacyValueDisallowAll(),
            PrivacyValueType.AllowContacts => new TPrivacyValueAllowContacts(),
            PrivacyValueType.DisallowContacts => new TPrivacyValueDisallowContacts(),
            PrivacyValueType.AllowUsers => new TPrivacyValueAllowUsers { Users = JsonSerializer.Deserialize<TVector<long>>(data.JsonData ?? "[]") ?? [] },
            PrivacyValueType.DisallowUsers => new TPrivacyValueDisallowUsers { Users = JsonSerializer.Deserialize<TVector<long>>(data.JsonData ?? "[]") ?? [] },
            _ => null
        };
    }

    private static PrivacyType MapKey(IInputPrivacyKey key)
    {
        return key switch
        {
            TInputPrivacyKeyStatusTimestamp => PrivacyType.StatusTimestamp,
            TInputPrivacyKeyPhoneNumber => PrivacyType.PhoneNumber,
            TInputPrivacyKeyProfilePhoto => PrivacyType.ProfilePhoto,
            _ => PrivacyType.StatusTimestamp
        };
    }
}