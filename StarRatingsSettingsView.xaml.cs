using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StarRatings
{
    public partial class StarRatingsSettingsView : UserControl
    {
        public StarRatingsSettingsView()
        {
            InitializeComponent();
        }

        private void HandleTextInputPreview_RatingSteps(object sender, TextCompositionEventArgs e)
        {
            // ensure all characters are text
            foreach (var ch in e.Text)
            {
                if (!Char.IsDigit(ch))
                {
                    e.Handled = true;
                    break;
                }
            }
        }
    }
}