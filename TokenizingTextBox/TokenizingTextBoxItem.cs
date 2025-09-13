using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace TokenizingTextBox;

/// <summary>
/// トークン表示用のアイテムコンテナ。
/// 各トークンに削除ボタンを表示し、クリックされたトークンを親 <see cref="TokenizingTextBox"/> から削除します。
/// </summary>
[TemplatePart(Name = s_removeButtonName, Type = typeof(Button))]
public class TokenizingTextBoxItem : ListBoxItem
{
    /// <summary>テンプレート内の削除ボタン名。</summary>
    private const string s_removeButtonName = "PART_RemoveButton";

    /// <summary>削除ボタン。</summary>
    private Button? _removeButton;

    static TokenizingTextBoxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TokenizingTextBoxItem), new FrameworkPropertyMetadata(typeof(TokenizingTextBoxItem)));
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_removeButton != null)
        {
            _removeButton.Click -= RemoveButton_Click;
        }

        _removeButton = GetTemplateChild(s_removeButtonName) as Button;

        if (_removeButton != null)
        {
            _removeButton.Click += RemoveButton_Click;
        }
    }

    /// <summary>
    /// 削除ボタンがクリックされたときの処理。
    /// 親 <see cref="TokenizingTextBox"/> から自身に対応するトークンを削除します。
    /// </summary>
    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsControl.ItemsControlFromItemContainer(this) is TokenizingTextBox owner)
        {
            var view = (IEditableCollectionView)owner.Items;
            var item = owner.ItemContainerGenerator.ItemFromContainer(this);
            if (item != DependencyProperty.UnsetValue)
            {
                view.Remove(item);
            }
        }

        e.Handled = true;
    }
}
