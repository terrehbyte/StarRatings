using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private StarRatingsSettingsViewModel SettingsViewModel { get; set; }
        private StarRatingsSettings CurrentSettings => SettingsViewModel.Settings;

        private readonly List<GameMenuItem> ratingMenuItems = new List<GameMenuItem>();
        private int lastIndexOfMenuItemWithCheck = -1;
        private readonly Dictionary<int, RatingData> scoreToRatingData = new Dictionary<int, RatingData>();

        public override Guid Id { get; } = Guid.Parse("1d6c5e6a-2198-4b40-b3a1-28fe46f5704a");

        public StarRatings(IPlayniteAPI api) : base(api)
        {
            SettingsViewModel = new StarRatingsSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        private class RatingData
        {
            public string ratingLabel { get; set; }
            public int ratingIndex { get; set; }
            public Guid ratingTagId { get; set; }
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            InitializeRatings();
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            if (CurrentSettings.ShouldApplyRatingTag)
            {
                yield return new MainMenuItem
                {
                    MenuSection = "StarRatings",
                    Description = "Rebuild Rating Tags",
                    Action = (menuArgs) => ApplyRatingTags(PlayniteApi.Database.Games.ToList()),
                };
                
                yield return new MainMenuItem
                {
                    MenuSection = "StarRatings",
                    Description = "Clear All Rating Tags",
                    Action = (menuArgs) => ClearRatingTags(PlayniteApi.Database.Games.ToList()),
                };
            }
        }
        
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            // clear old check if previously applied
            if (lastIndexOfMenuItemWithCheck != -1)
            {
                ratingMenuItems[lastIndexOfMenuItemWithCheck].Icon = null;
            }

            // show check only if all selected games have the same score
            //  (TODO: show multiple dots if multiple are applicable)
            bool hasUniformScore = false;
            int targetScore = -1;

            // initialize variables w/ first score if available, otherwise
            // don't show a score at all
            if (args.Games[0].UserScore.HasValue)
            {
                targetScore = args.Games[0].UserScore.Value;
                hasUniformScore = true;
            }

            // are the scores all the same?
            if (hasUniformScore)
            {
                for (int index = 1; index < args.Games.Count; index++)
                {
                    var game = args.Games[index];
                    if (game.UserScore.HasValue == false ||
                        game.UserScore.Value != targetScore)
                    {
                        hasUniformScore = false;
                        break;
                    }
                }
            }

            // identify score to place checkmark on
            if (hasUniformScore)
            {
                var curScore = args.Games[0].UserScore;
                int indexToModify = GetRatingIndexFromUserScore(CurrentSettings.RatingSteps, curScore.Value, CurrentSettings.EnableHalfStars, CurrentSettings.ShowZeroRating);
                if (indexToModify != -1)
                {
                    ratingMenuItems[indexToModify].Icon = PluginUtils.ResolveIconPath("check.png");

                    lastIndexOfMenuItemWithCheck = indexToModify;
                }
            }
            
            // return final menu list
            return ratingMenuItems;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return SettingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new StarRatingsSettingsView();
        }

        public void InitializeRatings()
        {
            ratingMenuItems.Clear();
            scoreToRatingData.Clear();
            
            // reload settings
            var curSettings = ((StarRatingsSettingsViewModel)GetSettings(false)).Settings;

            // add all steps
            int ratingIndex = 0;
            for (int i = curSettings.ShowZeroRating ? 0 : 1; i <= curSettings.RatingSteps; ++i)
            {
                int starLevel = i;
                int actualScore = GetUserScoreFromRating(curSettings.RatingSteps, starLevel);
                ratingMenuItems.Add(new GameMenuItem
                {
                    MenuSection = "Set Rating",
                    Description = $"{starLevel} Stars",
                    Action = (selectedGames) => ApplyUserScore(selectedGames.Games, actualScore)
                });
                scoreToRatingData[actualScore] =  new RatingData() { ratingIndex =  ratingIndex++, ratingLabel = $"{CurrentSettings.RatingTagPrefix}{starLevel} Stars" };

                // add one for the half-star?
                if (curSettings.EnableHalfStars && i < curSettings.RatingSteps)
                {
                    int actualHalfScore = GetUserScoreFromRating(curSettings.RatingSteps, starLevel, true);
                    ratingMenuItems.Add(new GameMenuItem
                    {
                        MenuSection = "Set Rating",
                        Description = $"{starLevel}.5 Stars",
                        Action = (selectedGames) => ApplyUserScore(selectedGames.Games, actualHalfScore)
                    });
                    scoreToRatingData[actualHalfScore] =  new RatingData() { ratingIndex =  ratingIndex++, ratingLabel = $"{CurrentSettings.RatingTagPrefix}{starLevel}.5 Stars" };
                }
            }
            
            // add reset if enabled
            if (curSettings.ShowReset)
            {
                // divider
                ratingMenuItems.Add(new GameMenuItem
                {
                    MenuSection = "Set Rating",
                    Description = $"-" // a "-" denotes a divider
                });
                
                // actual item
                ratingMenuItems.Add(new GameMenuItem
                {
                    MenuSection = "Set Rating",
                    Description = $"Clear Rating",
                    Icon = PluginUtils.ResolveIconPath("return.png"),
                    Action = (selectedGames) => ApplyUserScore(selectedGames.Games, null)
                });
            }

            lastIndexOfMenuItemWithCheck = -1;
            
            // make tags available if enabled
            if (curSettings.ShouldApplyRatingTag)
            {
                foreach (var ratingPair in scoreToRatingData)
                {
                    var rating = ratingPair.Value;

                    var ratingTag = PlayniteApi.Database.Tags.SingleOrDefault(x => x.Name == rating.ratingLabel);
                    if (ratingTag == null)
                    {
                        rating.ratingTagId = PlayniteApi.Database.Tags.Add(rating.ratingLabel).Id;
                    }
                    else
                    {
                        rating.ratingTagId = ratingTag.Id;
                    }
                }
            }
        }

        public int GetRatingIndexFromUserScore(int ratingSteps, int userScore, bool allowHalfRatings, bool allowZeroRating)
        {
            if (scoreToRatingData.ContainsKey(userScore))
            {
                return scoreToRatingData[userScore].ratingIndex;
            }

            // doesn't map
            return -1;
        }

        public static int GetUserScoreFromRating(int ratingSteps, int starLevel, bool isHalfRating = false)
        {
            int ratingStepSize = 100 / ratingSteps;
            return ratingStepSize * starLevel + (isHalfRating ? ratingStepSize/2 : 0);
        }
        
        private void ApplyUserScore(IEnumerable<Game> games, int? userScore)
        {
            foreach (Game game in games)
            {
                game.UserScore = userScore;

                if (CurrentSettings.ShouldApplyRatingTag && userScore.HasValue)
                {
                    // create tags if game has none
                    if (game.TagIds == null) { game.TagIds = new List<Guid>(1); }
                    game.TagIds.Add(scoreToRatingData[userScore.Value].ratingTagId);
                }
            }
            
            PlayniteApi.Database.Games.Update(games);
        }

        private void ApplyRatingTags(IEnumerable<Game> games)
        {
            foreach (var game in games)
            {
                // skip if no score
                if(!game.UserScore.HasValue) { continue; }

                int userScore = game.UserScore.Value;
                
                // skip if score does not correspond to anything
                // TODO: consider rounding down to next applicable score
                if (!scoreToRatingData.ContainsKey(userScore)) { continue; }

                // create TagIds for game if none exists
                if (game.TagIds == null)
                {
                    game.TagIds = new List<Guid>();
                }
                
                // retrieve and stage tag
                game.TagIds.Add(scoreToRatingData[userScore].ratingTagId);
            }
            
            PlayniteApi.Database.Games.Update(games);
        }
        
        private void ClearRatingTags(IEnumerable<Game> games)
        {
            // cache list of tags to remove
            List<Guid> tagsToRemove = new List<Guid>();
            foreach (var rating in scoreToRatingData)
            {
                tagsToRemove.Add(rating.Value.ratingTagId);
            }
            
            foreach (var game in games)
            {
                // skip if it doesn't have any tags
                if (game.TagIds == null) { continue; }
                
                // remove all known rating tags
                game.TagIds.RemoveAll(x => tagsToRemove.Contains(x));
            }
            
            PlayniteApi.Database.Games.Update(games);
        }
    }
}