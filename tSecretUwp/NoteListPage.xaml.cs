﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Tono.Gui.Uwp;
using tSecretCommon;
using tSecretCommon.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace tSecretUwp
{
    public sealed partial class NoteListPage : Page
    {
        public NoteListPage()
        {
            this.InitializeComponent();
        }

        private bool IsShowAll
        {
            get => ShowDeleted.IsChecked ?? false;
            set => ShowDeleted.IsChecked = value;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            log(null);
            Refresh();
            if (e.NavigationMode == NavigationMode.New)
            {
                ConfigUtil.Set("LoginUtc", DateTime.UtcNow.ToString());
            }
            if (e.NavigationMode == NavigationMode.Back)
            {
                var prevID = ConfigUtil.Get("PreviousNoteID", "--");
                var note = lvMain.Items.Select(a => (Note)a).Where(a => a.ID.ToString() == prevID).FirstOrDefault();
                if( note != default)
                {
                    DelayUtil.Start(TimeSpan.FromMilliseconds(100), () =>
                    {
                        lvMain.ScrollIntoView(note);
                        lvMain.SelectedItem = note;
                    });
                }
            }
            base.OnNavigatedTo(e);
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.Parameter is Note note)
            {
                ConfigUtil.Set("PreviousNoteID", note.ID.ToString());
            }
            else
            {
                if (e.SourcePageType != typeof(AuthPage))
                {
                    e.Cancel = true;
                }
            }
            base.OnNavigatingFrom(e);
        }

        private void Refresh()
        {
            // Delete empty instance
            var dels = new List<Note>();
            foreach (var note in NotePersister.Current)
            {
                if( string.IsNullOrEmpty(note.AccountID?.Trim()) &&
                    string.IsNullOrEmpty(note.Caption?.Trim()) &&
                    string.IsNullOrEmpty(note.Password?.Trim()))
                {
                    dels.Add(note);
                }
            }
            foreach (var note in dels)
            {
                NotePersister.Current.Remove(note);
            }

            var dat = new ObservableCollection<Note>();
            foreach (var note in NotePersister.Current
                                    .Where(rec => IsShowAll || rec.IsDeleted == false)
                                    .OrderBy(rec => $"{rec.CaptionRubi1}--{rec.CaptionRubi}")
            )
            {
                dat.Add(note);
            }
            lvMain.ItemsSource = dat;

            // Make index buttons
            IndexButtons.Children.Clear();

            foreach (var rubi1 in NotePersister.Current
                                    .Select(a => a.CaptionRubi1)
                                    .Where(a => a != "@")
                                    .Distinct()
                                    .OrderBy(a => a[0]))
            {
                Button btn;
                IndexButtons.Children.Add(btn = new Button
                {
                    Content = rubi1,
                    FontSize = 12,
                    Height = 28,
                    Margin = new Thickness { Left = -6, Top = -6, Right = -6, Bottom = -6 },
                    Foreground = new SolidColorBrush(Colors.Magenta),
                    Background = new SolidColorBrush(Colors.Transparent),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ClickMode = ClickMode.Press,
                    KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Auto,
                });
                btn.Click += Index_Click;
                if (Enum.TryParse<VirtualKey>(rubi1, out var key))
                {
                    btn.KeyboardAccelerators.Add(new KeyboardAccelerator
                    {
                        Key = key,
                    });
                }
            }
        }

        private void Index_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string caption)
            {
                for (var i = 0; i < lvMain.Items.Count; i++)
                {
                    var item = lvMain.Items[i] as Note;
                    if (item?.CaptionRubi1 == caption)
                    {
                        lvMain.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Inplementation of Sync event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Sync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                KurukuruIn();
                await syncProc();
                KurukuruOut();
                await new MessageDialog($"クラウドと同期しました。", "tSecret").ShowAsync();
            }
            catch (Exception ex)
            {
                await new MessageDialog($"クラウド同期に失敗しました。\r\n{ex.Message}", "tSecret").ShowAsync();
                log(null);
                KurukuruOut();
            }
        }

        /// <summary>
        /// Data syncronize between Local PC and Cloud storage
        /// </summary>
        /// <returns></returns>
        private async System.Threading.Tasks.Task syncProc()
        {
            log("Cloud sync...");
            await NotePersister.Current.Sync();
            Refresh();
            log(null);
        }

        private void KurukuruIn()
        {
            Kurukuru.Visibility = Visibility.Visible;
        }

        private void KurukuruOut()
        {
            Kurukuru.Visibility = Visibility.Collapsed;
        }

        private void log(string str)
        {
            if (str == null)
            {
                StatusBar.Text = "tSecret (c)2019-2020 Manabu Tonosaki Allrights reserved.";
            }
            else
            {
                StatusBar.Text = str;
            }
        }

        /// <summary>
        /// move to detail page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvMain_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(NoteEntryPage), lvMain.SelectedItem, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight,
            });
        }

        private void ShowDeleted_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            var note = new Note
            {
                ID = Guid.NewGuid(),
                CreatedDateTime = DateTime.Now,
            };
            NotePersister.Current.Add(note);
            Frame.Navigate(typeof(NoteEntryPage), note, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight,
            });
        }
    }
}
