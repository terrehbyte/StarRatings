using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarRatings
{
    public class StarRatingsSettings : ObservableObject
    {
        private int ratingSteps = 5;
        private bool showZeroRating = false;
        private bool showReset = false;
        private bool enableHalfStars = false;

        private bool shouldApplyRatingTag = false;
        private string ratingTagPrefix = "Rating: ";

        public int RatingSteps { get => ratingSteps; set => SetValue(ref ratingSteps, value); }
        public bool ShowZeroRating { get => showZeroRating; set => SetValue(ref showZeroRating, value); }
        public bool ShowReset { get => showReset; set => SetValue(ref showReset, value); }
        public bool EnableHalfStars { get => enableHalfStars; set => SetValue(ref enableHalfStars, value); }
        
        public bool ShouldApplyRatingTag { get => shouldApplyRatingTag; set => SetValue(ref shouldApplyRatingTag, value); }
        public string RatingTagPrefix { get => ratingTagPrefix; set => SetValue(ref ratingTagPrefix, value); }
        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
    }

    public class StarRatingsSettingsViewModel : ObservableObject, ISettings
    {
        private readonly StarRatings plugin;
        private StarRatingsSettings prevSavedSettings { get; set; }

        private StarRatingsSettings settings;
        public StarRatingsSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public StarRatingsSettingsViewModel(StarRatings plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<StarRatingsSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            Settings = savedSettings ?? new StarRatingsSettings();
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            prevSavedSettings = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = prevSavedSettings;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);

            bool hasChangedRatingScheme = prevSavedSettings.EnableHalfStars != Settings.EnableHalfStars ||
                                          prevSavedSettings.RatingSteps != Settings.RatingSteps ||
                                          prevSavedSettings.ShowZeroRating != Settings.ShowZeroRating ||
                                          prevSavedSettings.ShouldApplyRatingTag != Settings.ShouldApplyRatingTag ||
                                          prevSavedSettings.RatingTagPrefix != Settings.RatingTagPrefix;

            // did we have tags enabled before?
            if (hasChangedRatingScheme && prevSavedSettings.ShouldApplyRatingTag)
            {
                plugin.ClearRatingTags(plugin.PlayniteApi.Database.Games);
            }
            
            // Reinitialize ratings on edit
            // TODO - We could listen for actual changes but this should be fairly cheap
            //        and uncomplicated to maintain until we need more complexity
            plugin.InitializeRatings();
            
            // do we need to apply tags again?
            if (hasChangedRatingScheme && Settings.ShouldApplyRatingTag)
            {
                plugin.ReapplyRatingTags(plugin.PlayniteApi.Database.Games);
            }
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            
            // validate that we have a valid step count
            if(Settings.RatingSteps < 1)
            {
                errors.Add("Number of ratings options must be greater than zero.");
            }

            return errors.Count == 0;
        }
    }
}