using Playnite.SDK;
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

        public override Guid Id { get; } = Guid.Parse("1d6c5e6a-2198-4b40-b3a1-28fe46f5704a");

        public QuickRatings(IPlayniteAPI api) : base(api)
        {
            settings = new QuickRatingsSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = false
            };
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            // quick ratings
            for (int i = 1; i <= 5; ++i)
            {
                int starLevel = i;
                
                yield return new GameMenuItem
                {
                    MenuSection = "Set Rating",
                    Description = $"{starLevel} Stars",
                    Action = (selectedGames) =>
                    {
                        // apply rating change
                        foreach(var game in selectedGames.Games)
                        {
                            game.UserScore = GetIntegerRating(starLevel);
                        }
                        
                        // apply changes to db
                        PlayniteApi.Database.Games.Update(selectedGames.Games);
                    }
                };
            }
            
            // reset rating
            yield return new GameMenuItem
            {
                MenuSection = "Set Rating",
                Description = $"Clear Rating",
                Action = (selectedGames) =>
                {
                    // clear rating
                    foreach(var game in selectedGames.Games)
                    {
                        game.UserScore = null;
                    }
                    
                    // apply changes to db
                    PlayniteApi.Database.Games.Update(selectedGames.Games);
                }
            };
        }

        private int GetIntegerRating(int rating)
        {
            int step = 100 / 5;
            return step * rating;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new QuickRatingsSettingsView();
        }
    }
}