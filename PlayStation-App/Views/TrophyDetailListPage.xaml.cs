﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using PlayStation_App.Commands.Friends;
using PlayStation_App.Models.Friends;
using PlayStation_App.Models.Trophies;
using PlayStation_App.Models.TrophyDetail;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PlayStation_App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TrophyDetailListPage : Page
    {
        private Trophy _lastSelectedItem;
        public TrophyDetailListPage()
        {
            this.InitializeComponent();
        }

        private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
        {
            // Assure we are displaying the correct item. This is necessary in certain adaptive cases.
            TrophyListView.SelectedItem = _lastSelectedItem;
        }

        private void AdaptiveStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            UpdateForVisualState(e.NewState, e.OldState);
        }

        private void UpdateForVisualState(VisualState newState, VisualState oldState = null)
        {
            var isNarrow = newState == NarrowState;

            if (isNarrow && oldState == DefaultState && _lastSelectedItem != null)
            {
                // Resize down to the detail item. Don't play a transition.
                App.RootFrame.Navigate(typeof(TrophyDetailPage), null, new SuppressNavigationTransitionInfo());
            }

            EntranceNavigationTransitionInfo.SetIsTargetElement(MasterListView, isNarrow);
            if (DetailContentPresenter != null)
            {
                EntranceNavigationTransitionInfo.SetIsTargetElement(DetailContentPresenter, !isNarrow);
            }
        }

        private void TrophyDetail_OnClick(object sender, ItemClickEventArgs e)
        {
            var clickedItem = (Trophy)e.ClickedItem;
            if (clickedItem.TrophyHidden)
            {
                Locator.ViewModels.TrophiesVm.SelectedTrophy = null;
                Locator.ViewModels.TrophiesVm.IsTrophySelected = false;
                return;
            }
            _lastSelectedItem = clickedItem;
            Locator.ViewModels.TrophiesVm.SelectedTrophy = clickedItem;
            Locator.ViewModels.TrophiesVm.IsTrophySelected = true;
            if (AdaptiveStates.CurrentState == NarrowState)
            {
                // Use "drill in" transition for navigating from master list to detail view
                App.RootFrame.Navigate(typeof(TrophyDetailPage), null, new DrillInNavigationTransitionInfo());
            }
            else
            {
                // Play a refresh animation when the user switches detail items.
                //EnableContentTransitions();
            }
        }

        private async void PullToRefreshBox_OnRefreshInvoked(DependencyObject sender, object args)
        {
            await Locator.ViewModels.TrophiesVm.SetTrophyDetailList(Locator.ViewModels.TrophiesVm.NpcommunicationId);
        }
    }
}
