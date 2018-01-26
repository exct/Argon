using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

using MahApps.Metro.Controls;

using NetFwTypeLib;

namespace Argon
{
    public partial class FirewallUI : UserControl
    {

        public CollectionViewSource RulesListViewSource { get; set; }
        public FirewallUI()
        {
            InitializeComponent();
            LockdownState.IsChecked = Firewall.GetLockdownState();
            FirewallState.IsChecked = Firewall.GetEnabledState();
            RulesListViewSource = new CollectionViewSource();
            RefreshList(null, null);
            DataContext = this;
        }

        public class FirewallRule
        {
            public bool? Action { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
        }

        public void GetFirewallRules()
        {
            Task.Run(() =>
            {
                var rules = new List<FirewallRule>();

                foreach (INetFwRule rule in Firewall.FirewallPolicy.Rules.Cast<INetFwRule>().Where(x => x.Grouping == "Argon")) {
                    if (!rules.Exists(x => x.Name == rule.Name) && rule.Name != "Argon")
                        rules.Add(new FirewallRule
                        {
                            Action = rule.Action == NET_FW_ACTION_.NET_FW_ACTION_ALLOW ? true : false,
                            Name = rule.Name,
                            Path = rule.ApplicationName,
                        });
                }

                foreach (FirewallRule r in rules)
                    r.Name = r.Name.Split(new string[] { "__" }, StringSplitOptions.None)[0];

                foreach (FirewallRule app in GetAppsList())
                    if (!rules.Any(x => x.Name == app.Name && x.Path == app.Path))
                        rules.Add(app);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    RulesListViewSource.Source = rules;
                }));
            });

            List<FirewallRule> GetAppsList()
            {
                using (var db = new ArgonDB())
                    return db.NetworkTraffic
                             .Select(x => new FirewallRule
                             {
                                 Name = x.ApplicationName,
                                 Path = x.FilePath,
                                 Action = null
                             })
                             .Distinct()
                             .ToList();
            }
        }



        private void LockdownState_IsCheckedChanged(object sender, EventArgs e)
        {
            if (((ToggleSwitch)sender).IsChecked ?? false)
                Firewall.Lockdown();
            else
                Firewall.LockdownRelease();
        }


        private void RefreshList(object sender, System.Windows.RoutedEventArgs e)
        {
            GetFirewallRules();
        }

        private void ChangeAppFirewallRule(object sender, System.Windows.RoutedEventArgs e)
        {
            var rule = (FirewallRule)((FrameworkElement)sender).DataContext;

            switch (((ToggleButton)sender).IsChecked) {
                case true:
                    Firewall.RemoveRule(rule.Name + "__" + rule.Path);
                    Firewall.SetRule(rule.Name, rule.Path, true);
                    break;
                case false:
                    Firewall.RemoveRule(rule.Name + "__" + rule.Path);
                    Firewall.SetRule(rule.Name, rule.Path, false);
                    break;
                default:
                    Firewall.RemoveRule(rule.Name + "__" + rule.Path);
                    break;
            }
        }

        private void FirewallState_IsCheckedChanged(object sender, EventArgs e)
        {
            Firewall.EnableFirewall(FirewallState.IsChecked ?? true);
        }

        private void AppDetailsDataGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            txtInstructions.Visibility = Visibility.Visible;
        }

        private void AppDetailsDataGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            txtInstructions.Visibility = Visibility.Hidden;
        }
    }
}
