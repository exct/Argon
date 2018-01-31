using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

using MahApps.Metro.Controls;

namespace Argon
{
    public partial class FirewallUI : UserControl
    {

        public CollectionViewSource RulesListViewSource { get; set; }
        public FirewallUI()
        {
            InitializeComponent();
            RulesListViewSource = new CollectionViewSource();
            RefreshRuleList();
            LockdownState.IsChecked = !Firewall.GetLockdownState();
            FirewallState.IsChecked = Firewall.GetEnabledState();
            DataContext = this;
        }

        public void RefreshRuleList()
        {
            Task.Run(() =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    btnRefresh.IsEnabled = false;
                    RulesListViewSource.Source = null;
                    ProgressBar1.Visibility = Visibility.Visible;
                }));

                List<FirewallRule> rules = Firewall.GetFirewallRules();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    btnRefresh.IsEnabled = true;
                    ProgressBar1.Visibility = Visibility.Collapsed;
                    RulesListViewSource.Source = rules;
                }));
            });
        }


        private void LockdownState_IsCheckedChanged(object sender, EventArgs e)
        {
            if (!((ToggleSwitch)sender).IsChecked ?? false)
                Firewall.Lockdown();
            else
                Firewall.LockdownRelease();
        }


        private void RefreshList(object sender, System.Windows.RoutedEventArgs e)
        {
            RefreshRuleList();
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
