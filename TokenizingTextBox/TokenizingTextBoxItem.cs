using System.Windows;
using System.Windows.Controls;

namespace TokenizingTextBox;

public class TokenizingTextBoxItem : ListBoxItem
{
    static TokenizingTextBoxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TokenizingTextBoxItem), new FrameworkPropertyMetadata(typeof(TokenizingTextBoxItem)));
    }
}
