﻿using MahApps.Metro.Controls;
using NTMiner.Core;
using NTMiner.Notifications;
using NTMiner.Views.Ucs;
using NTMiner.Vms;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NTMiner.Views {
    public partial class MainWindow : MetroWindow, IMainWindow {
        private MainWindowViewModel Vm {
            get {
                return (MainWindowViewModel)this.DataContext;
            }
        }

        private static readonly object _locker = new object();
        private static MainWindow _instance = null;
        public static MainWindow Create() {
            if (_instance == null) {
                lock (_locker) {
                    if (_instance == null) {
                        _instance = new MainWindow();
                        NTMinerRoot.IsUiVisible = true;
                        NTMinerRoot.MainWindowRendedOn = DateTime.Now;
                        return _instance;
                    }
                    else {
                        return _instance;
                    }
                }
            }
            else {
                return _instance;
            }
        }

        private readonly List<Bus.IDelegateHandler> _handlers = new List<Bus.IDelegateHandler>();
        private MainWindow() {
            UIThread.StartTimer();
            InitializeComponent();
            this.StateChanged += (s, e) => {
                if (Vm.MinerProfile.IsShowInTaskbar) {
                    ShowInTaskbar = true;
                }
                else {
                    if (WindowState == WindowState.Minimized) {
                        ShowInTaskbar = false;
                    }
                    else {
                        ShowInTaskbar = true;
                    }
                }
            };
            EventHandler changeNotiCenterWindowLocation = Wpf.Util.ChangeNotiCenterWindowLocation(this);
            this.Activated += changeNotiCenterWindowLocation;
            this.LocationChanged += changeNotiCenterWindowLocation;
            if (!Windows.Role.IsAdministrator) {
                NotiCenterWindowViewModel.Instance.Manager
                    .CreateMessage()
                    .Warning("请以管理员身份运行。")
                    .WithButton("点击以管理员身份运行", button => {
                        AppContext.Current.RunAsAdministrator.Execute(null);
                    })
                    .Dismiss().WithButton("忽略", button => {
                        Vm.IsBtnRunAsAdministratorVisible = Visibility.Visible;
                    }).Queue();
            }
            if (NTMinerRoot.Instance.GpuSet.Count == 0) {
                NotiCenterWindowViewModel.Instance.Manager.ShowErrorMessage("没有矿卡或矿卡未驱动。");
            }
            VirtualRoot.On<StartingMineFailedEvent>("开始挖矿失败", LogEnum.DevConsole,
                action: message => {
                    Vm.MinerProfile.IsMining = false;
                    Write.UserFail(message.Message);
                }).AddToCollection(_handlers);
            if (DevMode.IsDevMode) {
                VirtualRoot.On<ServerJsonVersionChangedEvent>("开发者模式展示ServerJsonVersion", LogEnum.DevConsole,
                    action: message => {
                        Vm.ServerJsonVersion = Vm.GetServerJsonVersion();
                    }).AddToCollection(_handlers);
            }
        }

        protected override void OnClosed(EventArgs e) {
            foreach (var handler in _handlers) {
                VirtualRoot.UnPath(handler);
            }
            base.OnClosed(e);
            _instance = null;
        }

        public void ShowThisWindow(bool isToggle) {
            AppHelper.ShowWindow(this, isToggle);
        }

        private void MetroWindow_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                this.DragMove();
            }
        }

        private void BtnLeftTriangle_Click(object sender, RoutedEventArgs e) {
            BtnRightTriangle.Visibility = Visibility.Visible;
            BtnLayoutLeftRight.Visibility = Visibility.Visible;
            BtnLeftTriangle.Visibility = Visibility.Collapsed;
            BtnLayoutMain.Visibility = Visibility.Collapsed;
            MinerProfileContainerLeft.Visibility = Visibility.Collapsed;
            MinerProfileContainerLeft.Child = null;
            MinerProfileContainerRight.Child = GridMineStart;
            TabItemMinerProfile.Visibility = Visibility.Visible;
        }

        private void BtnRightTriangle_Click(object sender, RoutedEventArgs e) {
            BtnRightTriangle.Visibility = Visibility.Collapsed;
            BtnLayoutMain.Visibility = Visibility.Visible;
            BtnLeftTriangle.Visibility = Visibility.Visible;
            BtnLayoutLeftRight.Visibility = Visibility.Collapsed;
            MinerProfileContainerLeft.Visibility = Visibility.Visible;
            MinerProfileContainerRight.Child = null;
            MinerProfileContainerLeft.Child = GridMineStart;
            TabItemMinerProfile.Visibility = Visibility.Collapsed;
            if (TabItemMinerProfile.IsSelected) {
                TabItemLog.IsSelected = true;
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ConsoleUc == null) {
                return;
            }
            var selectedItem = ((TabControl)sender).SelectedItem;
            ConsoleUc.IsBuffer = selectedItem != TabItemLog;
            if (selectedItem == TabItemOuterProperty) {
                if (OuterPropertyContainer.Child == null) {
                    OuterPropertyContainer.Child = new OuterProperty();
                }
            }
            else if (selectedItem == TabItemToolbox) {
                if (ToolboxContainer.Child == null) {
                    ToolboxContainer.Child = new Toolbox();
                }
            }
            else if (selectedItem == TabItemMinerProfileOption) {
                if (MinerProfileOptionContainer.Child == null) {
                    MinerProfileOptionContainer.Child = new MinerProfileOption();
                }
            }
            else if (selectedItem == TabItemGpuOverClock) {
                if (GpuOverClockContainer.Child == null) {
                    GpuOverClockContainer.Child = new GpuOverClock();
                }
            }
            else if (selectedItem == TabItemSpeedTable) {
                if (SpeedTableContainer.Child == null) {
                    SpeedTableContainer.Child = new SpeedTable();
                }
            }
        }
    }
}
