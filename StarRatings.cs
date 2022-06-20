using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace StarRatings
{
    public class StarRatings : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private StarRatingsSettingsViewModel settings { get; set; }

        private List<GameMenuItem> ratingMenuItems = new List<GameMenuItem>();
        private int ratingStepSize = -1;
        
        public override Guid Id { get; } = Guid.Parse("1d6c5e6a-2198-4b40-b3a1-28fe46f5704a");

        public StarRatings(IPlayniteAPI api) : base(api)
        {
            settings = new StarRatingsSettingsViewModel(this);
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
            return ratingMenuItems;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new StarRatingsSettingsView();
        }

        public void InitializeRatings()
        {
            ratingMenuItems.Clear();
            
            var curSettings = ((StarRatingsSettingsViewModel)GetSettings(false)).Settings;
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

                // add one for the half-star?
                if (curSettings.EnableHalfStars && i < curSettings.RatingSteps)
                {
                    ratingMenuItems.Add(new GameMenuItem
                    {
                        MenuSection = "Set Rating",
                        Description = $"{starLevel}.5 Stars",
                        Action = (selectedGames) =>
                        {
                            // apply rating change
                            foreach (var game in selectedGames.Games)
                            {
                                game.UserScore = ratingStepSize * starLevel + ratingStepSize/2;
                            }

                            // apply changes to db
                            PlayniteApi.Database.Games.Update(selectedGames.Games);
                        }   
                    });
                }
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