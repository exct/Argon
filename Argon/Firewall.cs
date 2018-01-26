using System;
using System.Diagnostics;
using System.Linq;

using NetFwTypeLib;

namespace Argon
{
    public sealed class Firewall
    {
        public static INetFwPolicy2 FirewallPolicy
        {
            get {
                if (_firewallPolicy == null)
                    _firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                return _firewallPolicy;
            }
        }
        private static INetFwPolicy2 _firewallPolicy;
        private static INetFwRule ArgonFirewallRule
        {
            get {
                INetFwRule rule = GetNewRule();
                rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                rule.Enabled = true;
                rule.InterfaceTypes = "All";
                rule.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
                rule.Grouping = "Argon";

                rule.Name = "Argon";
                rule.ApplicationName = Process.GetCurrentProcess().MainModule.FileName;

                return rule;
            }
        }

        public static void Initialize()
        {
            if (!FirewallPolicy.Rules.Cast<INetFwRule>().Any(x => x.Grouping == "Argon"))
                FirewallPolicy.Rules.Add(ArgonFirewallRule);
        }

        public static void SetRule(string applicationName, string path, bool allow)
        {
            INetFwRule firewallRuleOUT = GetNewRule();
            INetFwRule firewallRuleIN = GetNewRule();
            firewallRuleOUT.Action = firewallRuleIN.Action = allow ? NET_FW_ACTION_.NET_FW_ACTION_ALLOW : NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            firewallRuleOUT.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
            firewallRuleIN.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            firewallRuleOUT.Enabled = firewallRuleIN.Enabled = true;
            firewallRuleOUT.InterfaceTypes = firewallRuleIN.InterfaceTypes = "All";
            firewallRuleOUT.Profiles = firewallRuleIN.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
            firewallRuleOUT.Grouping = firewallRuleIN.Grouping = "Argon";

            firewallRuleOUT.Name = firewallRuleIN.Name = applicationName + "__" + path;
            firewallRuleOUT.ApplicationName = firewallRuleIN.ApplicationName = path;

            FirewallPolicy.Rules.Add(firewallRuleOUT);
            FirewallPolicy.Rules.Add(firewallRuleIN);

        }

        public static void RemoveRule(string ruleName)
        {
            while (FirewallPolicy.Rules.Cast<INetFwRule>()
                                       .Any(x => x.Name == ruleName)) {
                FirewallPolicy.Rules.Remove(ruleName);
                FirewallPolicy.Rules.Remove(ruleName);
            }
        }

        public static void Lockdown()
        {
            INetFwRule firewallRuleOut = GetNewRule();
            INetFwRule firewallRuleIn = GetNewRule();
            firewallRuleOut.Action = firewallRuleIn.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            firewallRuleOut.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
            firewallRuleIn.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            firewallRuleOut.Enabled = firewallRuleIn.Enabled = true;
            firewallRuleOut.InterfaceTypes = firewallRuleIn.InterfaceTypes = "All";
            firewallRuleOut.Profiles = firewallRuleIn.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
            firewallRuleOut.Name = "Argon - Lockdown Outbound Connections";
            firewallRuleIn.Name = "Argon - Lockdown Inbound Connections";

            FirewallPolicy.Rules.Add(firewallRuleOut);
            FirewallPolicy.Rules.Add(firewallRuleIn);
        }

        public static void LockdownRelease()
        {
            while (FirewallPolicy.Rules.Cast<INetFwRule>()
                                       .Any(x => x.Name == "Argon - Lockdown Inbound Connections"
                                              || x.Name == "Argon - Lockdown Outbound Connections")) {
                FirewallPolicy.Rules.Remove("Argon - Lockdown Outbound Connections");
                FirewallPolicy.Rules.Remove("Argon - Lockdown Inbound Connections");
            }
        }

        public static bool GetLockdownState()
        {
            return FirewallPolicy.Rules.Cast<INetFwRule>()
                                       .Any(x => x.Name == "Lockdown Inbound Connections"
                                              || x.Name == "Lockdown Outbound Connections");
        }

        public static bool GetEnabledState()
        {
            return FirewallPolicy.IsRuleGroupEnabled((int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL, "Argon");
        }

        public static INetFwRule GetNewRule()
        {
            switch (App.Current.Properties["WindowsVersion"]) {
                case "7":
                case "8":
                    return (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                case "10":
                    return (INetFwRule3)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                default:
                    return (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            }
        }

        public static void EnableFirewall(bool enable)
        {
            FirewallPolicy.EnableRuleGroup((int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL, "Argon", enable);
        }
    }
}
