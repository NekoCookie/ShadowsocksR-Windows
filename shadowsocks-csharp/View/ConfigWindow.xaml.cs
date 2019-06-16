using Shadowsocks.Controller;
using Shadowsocks.Encryption;
using Shadowsocks.Model;
using Shadowsocks.Util;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Shadowsocks.View
{
    public partial class ConfigWindow
    {
        public ConfigWindow(ShadowsocksController controller, int focusIndex)
        {
            InitializeComponent();
            Splitter1.DragDelta += SplitterNameDragDelta;

            Closed += (o, e) => { _controller.ConfigChanged -= controller_ConfigChanged; };
            _controller = controller;
            foreach (var name in EncryptorFactory.GetEncryptor().Keys)
            {
                var info = EncryptorFactory.GetEncryptorInfo(name);
                if (info.display)
                {
                    EncryptionComboBox.Items.Add(name);
                }
            }
            foreach (var protocol in Protocols)
            {
                ProtocolComboBox.Items.Add(protocol);
            }
            foreach (var obfs in ObfsStrings)
            {
                ObfsComboBox.Items.Add(obfs);
            }

            LoadCurrentConfiguration();
            if (_modifiedConfiguration.index >= 0 &&
                _modifiedConfiguration.index < _modifiedConfiguration.configs.Count)
            {
                _oldSelectedId = _modifiedConfiguration.configs[_modifiedConfiguration.index].id;
            }

            if (focusIndex == -1)
            {
                var index = _modifiedConfiguration.index + 1;
                if (index < 0 || index > _modifiedConfiguration.configs.Count)
                    index = _modifiedConfiguration.configs.Count;

                focusIndex = index;
            }

            //Opacity = 1;
            //Show();

            if (focusIndex >= 0 && focusIndex < _modifiedConfiguration.configs.Count)
            {
                SetServerListSelectedIndex(focusIndex);
                LoadSelectedServer();
            }

            UpdateServersListBoxTopIndex();
        }

        private static readonly string[] Protocols = {
                "origin",
                "verify_deflate",
                "auth_sha1_v4",
                "auth_aes128_md5",
                "auth_aes128_sha1",
                "auth_chain_a",
                "auth_chain_b",
                "auth_chain_c",
                "auth_chain_d",
                "auth_chain_e",
                "auth_chain_f",
                "auth_akarin_rand",
                "auth_akarin_spec_a"
        };

        private static readonly string[] ObfsStrings = {
                "plain",
                "http_simple",
                "http_post",
                "random_head",
                "tls1.2_ticket_auth",
                "tls1.2_ticket_fastauth"
        };

        private void SplitterNameDragDelta(object sender, DragDeltaEventArgs e)
        {
            MainGrid.ColumnDefinitions[1].Width = new GridLength(MainGrid.ColumnDefinitions[1].ActualWidth);
            Width += e.HorizontalChange;
        }

        private readonly ShadowsocksController _controller;
        private Configuration _modifiedConfiguration;
        private int _oldSelectedIndex = -1;
        private bool _allowSave = true;
        private bool _ignoreLoad;
        private readonly string _oldSelectedId;

        private string _SelectedID;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLanguage();
            _controller.ConfigChanged += controller_ConfigChanged;
            //TODO: ApplyButton.IsEnabled = true;

            ServerIpTextBox.Focus();
            ApplyButton.IsEnabled = false;
        }

        private void LoadLanguage()
        {
            Title = $@"{I18N.GetString(@"Edit Servers")}({(_controller.GetCurrentConfiguration().shareOverLan ? I18N.GetString(@"Any") : I18N.GetString(@"Local"))}:{_controller.GetCurrentConfiguration().localPort} {I18N.GetString(@"Version")}:{UpdateChecker.FullVersion})";

            foreach (var c in Utils.FindVisualChildren<Label>(this))
            {
                c.Content = I18N.GetString(c.Content.ToString());
            }

            foreach (var c in Utils.FindVisualChildren<TextBlock>(this))
            {
                c.Text = I18N.GetString(c.Text);
            }

            foreach (var c in Utils.FindVisualChildren<Button>(this))
            {
                c.Content = I18N.GetString(c.Content.ToString());
            }

            foreach (var c in Utils.FindVisualChildren<CheckBox>(this))
            {
                c.Content = I18N.GetString(c.Content.ToString());
            }

            foreach (var c in Utils.FindVisualChildren<GroupBox>(this))
            {
                c.Header = I18N.GetString(c.Header.ToString());
            }
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = _controller.GetConfiguration();
            LoadConfiguration();
            _allowSave = false;
            SetServerListSelectedIndex(_modifiedConfiguration.index);
            _allowSave = true;
            LoadSelectedServer();
        }

        private void LoadConfiguration()
        {
            if (ServersListBox.Items.Count != _modifiedConfiguration.configs.Count)
            {
                ServersListBox.Items.Clear();
                foreach (var server in _modifiedConfiguration.configs)
                {
                    if (!string.IsNullOrEmpty(server.group))
                    {
                        ServersListBox.Items.Add(server.group + " - " + server.HiddenName());
                    }
                    else
                    {
                        ServersListBox.Items.Add("      " + server.HiddenName());
                    }
                }
            }
            else
            {
                for (var i = 0; i < _modifiedConfiguration.configs.Count; ++i)
                {
                    if (!string.IsNullOrEmpty(_modifiedConfiguration.configs[i].group))
                    {
                        ServersListBox.Items[i] = _modifiedConfiguration.configs[i].group + " - " + _modifiedConfiguration.configs[i].HiddenName();
                    }
                    else
                    {
                        ServersListBox.Items[i] = "      " + _modifiedConfiguration.configs[i].HiddenName();
                    }
                }
            }
        }

        private void UpdateServersListBoxTopIndex(int style = 0)
        {
            //int visibleItems = ServersListBox.Height / ServersListBox.ItemHeight;
            //int index;
            //if (style == 0)
            //{
            //    index = ServersListBox.SelectedIndex;
            //}
            //else
            //{
            //    var items = ServersListBox.SelectedIndices;
            //    if (0 == items.Count)
            //        index = 0;
            //    else
            //        index = (style == 1 ? items[0] : items[items.Count - 1]);
            //}

            //int topIndex = Math.Max(index - visibleItems / 2, 0);
            //ServersListBox.TopIndex = topIndex;
        }

        private void LoadSelectedServer()
        {
            if (ServersListBox.SelectedIndex >= 0 && ServersListBox.SelectedIndex < _modifiedConfiguration.configs.Count)
            {
                var server = _modifiedConfiguration.configs[ServersListBox.SelectedIndex];

                ServerIpTextBox.Text = server.server;
                ServerPortNumber.NumValue = server.server_port;
                UdpPortNumber.NumValue = server.server_udp_port;
                PasswordTextBox.Text = server.password;
                EncryptionComboBox.Text = server.method ?? @"aes-256-cfb";
                if (string.IsNullOrEmpty(server.protocol))
                {
                    ProtocolComboBox.Text = @"origin";
                }
                else
                {
                    ProtocolComboBox.Text = server.protocol ?? @"origin";
                }
                var obfsText = server.obfs ?? @"plain";
                ObfsComboBox.Text = obfsText;
                ProtocolParamTextBox.Text = server.protocolparam;
                ObfsParamTextBox.Text = server.obfsparam;
                RemarksTextBox.Text = server.remarks;
                GroupTextBox.Text = server.group;
                UdpOverTcpCheckBox.IsChecked = server.udp_over_tcp;
                _SelectedID = server.id;

                ServerGroupBox.Visibility = Visibility.Visible;

                if (ProtocolComboBox.Text == @"origin"
                    && obfsText == @"plain"
                    && !UdpOverTcpCheckBox.IsChecked.GetValueOrDefault()
                    )
                {
                    AdvSettingCheckBox.IsChecked = false;
                }

                LinkTextBox.Text = SsrLinkCheckBox.IsChecked.GetValueOrDefault() ? server.GetSSRLinkForServer() : server.GetSSLinkForServer();

                if (UdpOverTcpCheckBox.IsChecked.GetValueOrDefault() || server.server_udp_port != 0)
                {
                    AdvSettingCheckBox.IsChecked = true;
                }

                UpdateObfsTextBox();
                LinkTextBox.SelectAll();
            }
            else
            {
                ServerGroupBox.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateObfsTextBox()
        {
            try
            {
                var obfs = (Obfs.ObfsBase)Obfs.ObfsFactory.GetObfs(ObfsComboBox.Text);
                var properties = obfs.GetObfs()[ObfsComboBox.Text];
                ObfsParamTextBox.IsEnabled = properties[2] > 0;
            }
            catch
            {
                ObfsParamTextBox.IsEnabled = true;
            }
        }

        public void SetServerListSelectedIndex(int index)
        {
            ServersListBox.UnselectAll();
            if (index < ServersListBox.Items.Count)
            {
                ServersListBox.SelectedIndex = index;
            }
            else
            {
                _oldSelectedIndex = ServersListBox.SelectedIndex;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyButton.IsEnabled = false;
        }
    }
}
