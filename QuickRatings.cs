﻿using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickRatings
{
    public class QuickRatings : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private QuickRatingsSettingsViewModel settings { get; set; }

        private List<GameMenuItem> ratingMenuItems = new List<GameMenuItem>();
        private int ratingStepSize = -1;
        
        public override Guid Id { get; } = Guid.Parse("1d6c5e6a-2198-4b40-b3a1-28fe46f5704a");

        public QuickRatings(IPlayniteAPI api) : base(api)
        {
            settings = new QuickRatingsSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            InitializeRatings();
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            foreach (var item in ratingMenuItems)
            {
                yield return item;
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new QuickRatingsSettingsView();
        }

        public void InitializeRatings()
        {
            ratingMenuItems.Clear();

            var curSettings = ((QuickRatingsSettingsViewModel)GetSettings(false)).Settings;
            ratingStepSize = 100 / curSettings.RatingSteps;
            
            // add all steps
            for (int i = curSettings.ShowZeroRating ? 0 : 1; i <= curSettings.RatingSteps; ++i)
            {
                int starLevel = i;
                ratingMenuItems.Add(new GameMenuItem
                {
                    MenuSection = "Set Rating",
                    Description = $"{starLevel} Stars",
                    Action = (selectedGames) =>
                    {
                        // apply rating change
                        foreach (var game in selectedGames.Games)
                        {
                            game.UserScore = ratingStepSize * starLevel;
                        }

                        // apply changes to db
                        PlayniteApi.Database.Games.Update(selectedGames.Games);
                    }
                });
            }
            
            // add reset if enabled
            if (curSettings.ShowReset)
            {
                ratingMenuItems.Add(new GameMenuItem
                {
                    MenuSection = "Set Rating",
                    Description = $"Clear Rating",
                    Action = (selectedGames) =>
                    {
                        // clear rating
                        foreach (var game in selectedGames.Games)
                        {
                            game.UserScore = null;
                        }

                        // apply changes to db
                        PlayniteApi.Database.Games.Update(selectedGames.Games);
                    }
                });
            }
        }
    }
}