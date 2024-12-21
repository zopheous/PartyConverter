using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using ECommons;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyConverter;

public sealed class PartyConverter : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;


    private TaskManager TaskManager;
    private const string CommandName = "/pconvert";


    public PartyConverter()
    {
        ECommonsMain.Init(PluginInterface, this);
        TaskManager = new(new(timeLimitMS: 60 * 1000, showDebug: true));
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Convert current party to cross-world party"
        });
    }

    public void Dispose()
    {
        ECommonsMain.Dispose();
        CommandManager.RemoveHandler(CommandName);
    }

    private unsafe void OnCommand(string command, string args)
    {
        TaskManager.Abort();
        if (!Player.Available)
        {
            return;
        }
        if (Player.Object.OnlineStatus.RowId == 26)
        {
            // Recruiting
            return;
        }
        var cfg = new TaskManagerConfiguration(timeLimitMS: 5000);
        TaskManager.Enqueue(() =>
        {
            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd"))
            {
                Chat.Instance.ExecuteCommand("/pfinder");
            }
        }, "Close pfinder", cfg);
        TaskManager.Enqueue(() => !GenericHelpers.TryGetAddonByName<AtkUnitBase>("LookingForGroup", out _), cfg);
        TaskManager.Enqueue(() =>
        {
            Chat.Instance.ExecuteCommand("/pfinder");
        }, "Open pfinder", cfg);
        TaskManager.Enqueue(() =>
        {
            if (GenericHelpers.TryGetAddonMaster<AddonMaster.LookingForGroup>(out var lfg) && GenericHelpers.IsAddonReady(lfg.Base) && EzThrottler.Throttle("RMOD"))
            {
                return lfg.RecruitMembersOrDetails();
            }
            return false;
        }, "Open recruitment", cfg);
        TaskManager.Enqueue(() =>
        {
            if (GenericHelpers.TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && GenericHelpers.IsAddonReady(m.Base))
            {
                m.Normal();
                return true;
            }
            return false;
        }, "Select tab", cfg);
        TaskManager.Enqueue(() =>
        {
            if (GenericHelpers.TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && GenericHelpers.IsAddonReady(m.Base))
            {
                m.SelectDutyCategory(0);
                return true;
            }
            return false;
        }, "Select category", cfg);
        TaskManager.Enqueue(() =>
        {
            if (GenericHelpers.TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && GenericHelpers.IsAddonReady(m.Base) && EzThrottler.Throttle("Recruit", 2000))
            {
                return m.Recruit();
            }
            return false;
        }, "Recruit", cfg);


        TaskManager.Enqueue(() => !GenericHelpers.TryGetAddonByName<AtkUnitBase>("LookingForGroup", out _), "Check UI", cfg);
        TaskManager.Enqueue(() =>
        {
            Chat.Instance.ExecuteCommand("/pfinder");
        }, "Re-open pfinder", cfg);
        TaskManager.Enqueue(() =>
        {
            if (GenericHelpers.TryGetAddonMaster<AddonMaster.LookingForGroup>(out var lfg) && GenericHelpers.IsAddonReady(lfg.Base) && EzThrottler.Throttle("RMOD2"))
            {
                return lfg.RecruitMembersOrDetails();
            }
            return false;
        }, "Open recruitment details", cfg);
        TaskManager.Enqueue(() =>
        {
            if (GenericHelpers.TryGetAddonMaster<AddonMaster.LookingForGroupDetail>(out var m) && GenericHelpers.IsAddonReady(m.Base) && EzThrottler.Throttle("End", 1000))
            {
                return m.TellEnd();
            }
            return false;
        }, "End recruitment", cfg);
        TaskManager.Enqueue(() =>
        {
            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd3"))
            {
                Chat.Instance.ExecuteCommand("/pfinder");
                return true;
            }
            return false;
        });
        return;

    }
}
