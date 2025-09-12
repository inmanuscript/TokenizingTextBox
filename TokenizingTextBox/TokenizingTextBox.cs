using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TokenizingTextBox;

/// <summary>
/// TokenizingTextBoxは、入力テキストを区切り文字で分割し、トークン（タグやラベル等）として管理・表示できるListBox派生コントロールです。
/// ユーザーがテキストボックスに入力した内容を、区切り文字やEnter/Tabキー等で自動的に分割・追加します。
/// トークンの追加・削除、フォーカス制御、キーボード操作などを柔軟にサポートします。
/// </summary>
[TemplatePart(Name = s_textBoxName, Type = typeof(TextBox))]
[TemplatePart(Name = s_wrapPanelName, Type = typeof(Panel))]
public class TokenizingTextBox : ListBox
{
    #region Dependency Properties

    #region Property Keys

    /// <summary>
    /// ユーザーがトークンを追加できるかどうかを示す読み取り専用プロパティのキー
    /// </summary>
    private static readonly DependencyPropertyKey s_canUserAddPropertyKey = DependencyProperty.RegisterReadOnly(nameof(CanUserAdd), typeof(bool), typeof(TokenizingTextBox), null);
    /// <summary>
    /// ユーザーがトークンを削除できるかどうかを示す読み取り専用プロパティのキー
    /// </summary>
    private static readonly DependencyPropertyKey s_canUserRemovePropertyKey = DependencyProperty.RegisterReadOnly(nameof(CanUserRemove), typeof(bool), typeof(TokenizingTextBox), null);

    #endregion Property Keys

    #region Properties

    /// <summary>
    /// テキストボックスで改行（Enterキー）を受け付けるかどうか
    /// </summary>
    public static readonly DependencyProperty AcceptsReturnProperty = KeyboardNavigation.AcceptsReturnProperty.AddOwner(typeof(TokenizingTextBox));
    /// <summary>
    /// テキストボックスでTabキーを受け付けるかどうか
    /// </summary>
    public static readonly DependencyProperty AcceptsTabProperty = DependencyProperty.Register("AcceptsTab", typeof(bool), typeof(TokenizingTextBox));
    /// <summary>
    /// テキストボックスのフォーカスが外れたときにトークンを追加するかどうか
    /// </summary>
    public static readonly DependencyProperty AddOnFocusLostProperty = DependencyProperty.Register("AddOnFocusLost", typeof(bool), typeof(TokenizingTextBox));
    /// <summary>
    /// ユーザーがトークンを追加できるかどうか（内部管理用）
    /// </summary>
    public static readonly DependencyProperty CanUserAddProperty = s_canUserAddPropertyKey.DependencyProperty;
    /// <summary>
    /// ユーザーがトークンを削除できるかどうか（内部管理用）
    /// </summary>
    public static readonly DependencyProperty CanUserRemoveProperty = s_canUserRemovePropertyKey.DependencyProperty;
    /// <summary>
    /// トークンの区切り文字（デフォルトは半角スペース）
    /// </summary>
    public static readonly DependencyProperty TokenDelimiterProperty = DependencyProperty.Register(
        nameof(TokenDelimiter),
        typeof(string),
        typeof(TokenizingTextBox),
        new PropertyMetadata(" "));

    #endregion Properties

    #region Accessors

    /// <summary>
    /// テキストボックスで改行（Enterキー）を受け付けるかどうか
    /// </summary>
    public bool AcceptsReturn
    {
        get { return (bool)GetValue(AcceptsReturnProperty); }
        set { SetValue(AcceptsReturnProperty, value); }
    }

    /// <summary>
    /// テキストボックスでTabキーを受け付けるかどうか
    /// </summary>
    public bool AcceptsTab
    {
        get { return (bool)GetValue(AcceptsTabProperty); }
        set { SetValue(AcceptsTabProperty, value); }
    }

    /// <summary>
    /// テキストボックスのフォーカスが外れたときにトークンを追加するかどうか
    /// </summary>
    public bool AddOnFocusLost
    {
        get { return (bool)GetValue(AddOnFocusLostProperty); }
        set { SetValue(AddOnFocusLostProperty, value); }
    }

    /// <summary>
    /// ユーザーがトークンを追加できるかどうか
    /// </summary>
    public bool CanUserAdd => (bool)GetValue(CanUserAddProperty);

    /// <summary>
    /// ユーザーがトークンを削除できるかどうか
    /// </summary>
    public bool CanUserRemove => (bool)GetValue(CanUserRemoveProperty);

    /// <summary>
    /// トークンの区切り文字（デフォルトは半角スペース）
    /// </summary>
    public string TokenDelimiter
    {
        get => (string)GetValue(TokenDelimiterProperty);
        set => SetValue(TokenDelimiterProperty, value);
    }

    #endregion Accessors

    #endregion Dependency Properties

    /// <summary>
    /// コントロールテンプレート内のTextBox部品名
    /// </summary>
    private const string s_textBoxName = "PART_TextBox";
    /// <summary>
    /// コントロールテンプレート内のWrapPanel部品名
    /// </summary>
    private const string s_wrapPanelName = "PART_WrapPanel";
    /// <summary>
    /// テンプレートから取得したTextBoxインスタンス
    /// </summary>
    private TextBox? _textBox;
    /// <summary>
    /// テンプレートから取得したWrapPanelインスタンス
    /// </summary>
    private WrapPanel? _wrapPanel;

    /// <summary>
    /// 静的コンストラクタ。デフォルトスタイルキーや選択モードの初期化を行う。
    /// </summary>
    static TokenizingTextBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TokenizingTextBox), new FrameworkPropertyMetadata(typeof(TokenizingTextBox)));

        SelectionModeProperty.OverrideMetadata(typeof(TokenizingTextBox), new FrameworkPropertyMetadata(SelectionMode.Extended));
    }

    /// <summary>
    /// コントロールテンプレート適用時の初期化処理。
    /// テンプレート部品の取得とイベントハンドラの登録・解除を行う。
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // 既存のTextBoxイベント購読解除
        if (_textBox != null)
        {
            _textBox.Loaded -= OnASBLoaded;

            _textBox.TextChanged -= TextBox_TextChanged;
            _textBox.GotKeyboardFocus -= TextBox_GotKeyboardFocus;
            _textBox.LostKeyboardFocus -= TextBox_LostKeyboardFocus;
        }

        // テンプレート部品取得
        _textBox = (TextBox)GetTemplateChild(s_textBoxName);
        _wrapPanel = (WrapPanel)GetTemplateChild(s_wrapPanelName);

        // TextBoxイベント購読
        if (_textBox != null)
        {
            _textBox.Loaded += OnASBLoaded;

            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
            _textBox.PreviewKeyDown += this.TextBox_PreviewKeyDown; // nullチェック済み
        }
    }

    /// <summary>
    /// トークン表示用のアイテムコンテナ（TokenizingTextBoxItem）を生成
    /// </summary>
    protected override DependencyObject GetContainerForItemOverride() => new TokenizingTextBoxItem();

    /// <summary>
    /// ItemsSource変更時の処理。トークン追加・削除可否を更新。
    /// </summary>
    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);

        IEditableCollectionViewAddNewItem view = Items;
        SetValue(s_canUserAddPropertyKey, view.CanAddNewItem);
        SetValue(s_canUserRemovePropertyKey, view.CanRemove);
    }

    /// <summary>
    /// キーボード操作（Delete/Back）によるトークン削除処理。
    /// 選択中のトークンを降順で削除し、残ったトークンにフォーカスを移す。
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Handled) { base.OnKeyDown(e); return; }

        bool goBack;
        switch (e.Key)
        {
            case Key.Delete: goBack = false; break;
            case Key.Back: goBack = true; break;
            default: base.OnKeyDown(e); return;
        }

        if (!CanUserRemove) { e.Handled = true; return; }

        // 1) 選択中のインデックスを取って降順で削除
        var selectedIndices = this.SelectedItems
            .Cast<object>()
            .Select(item => this.Items.IndexOf(item))
            .Where(i => i >= 0)
            .Distinct()
            .OrderByDescending(i => i)
            .ToList();

        if (selectedIndices.Count == 0) { e.Handled = true; return; }

        int smallestIndex = selectedIndices.Min();

        var view = (IEditableCollectionView)this.Items;
        foreach (var idx in selectedIndices)
            view.RemoveAt(idx);

        // 2) 選択クリア
        this.SelectedItems.Clear();

        // 3) 残っていればフォーカス移動
        if (this.Items.Count > 0)
        {
            var selectIndex = smallestIndex;
            if (goBack) selectIndex = Math.Max(0, selectIndex - 1);
            selectIndex = Math.Min(selectIndex, this.Items.Count - 1);

            if (this.ItemContainerGenerator.ContainerFromIndex(selectIndex) is ListBoxItem lbi)
            {
                lbi.IsSelected = true;
                lbi.Focus();
            }
        }
        else
        {
            _textBox?.Focus();
        }

        e.Handled = true;
        base.OnKeyDown(e);
    }

    /// <summary>
    /// 指定した文字列をトークンとして追加
    /// </summary>
    /// <param name="token">追加するトークン文字列</param>
    private void AddToken(string token)
    {
        token = token.Trim();

        if (token.Length > 0)
        {
            IEditableCollectionViewAddNewItem items = Items;
            items.AddNewItem(token);
            items.CommitNew();
        }
    }

    /// <summary>
    /// TextBoxの内容をトークンとして追加
    /// </summary>
    private void AddToken()
    {
        if (_textBox == null) return;
        var text = _textBox.Text;
        _textBox.Text = String.Empty;
        AddToken(text);
    }

    /// <summary>
    /// TextBoxのLoadedイベントハンドラ。TextBoxの再取得とイベント再登録を行う。
    /// テンプレート変更時にも対応。
    /// </summary>
    private void OnASBLoaded(object sender, RoutedEventArgs e)
    {
        // 1) 古い購読を解除
        if (_textBox != null)
        {
            _textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
            _textBox = null;
        }

        // 2) 今の TextBox を取り直す
        var asb = (Control)sender;
        if (asb.Name == s_textBoxName)
        {
            _textBox = (TextBox)asb;
        }
        else
        {
            // テンプレートが変わって TextBox が入れ替わることがあるので念のため探す
            asb.ApplyTemplate(); // 念のため
            var tb = asb.Template.FindName(s_textBoxName, asb) as TextBox
                     ?? FindDescendant<TextBox>(asb); // 保険
            _textBox = tb;

        }

        // 3) 付け直し
        if (_textBox != null)
        {
            _textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
        }
    }

    /// <summary>
    /// TextBoxのUnloadedイベントハンドラ。イベント購読解除。
    /// </summary>
    private void OnASBUnloaded(object sender, RoutedEventArgs e)
    {
        if (_textBox != null)
        {
            _textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
            _textBox = null;
        }
    }

    /// <summary>
    /// 指定した型の子孫要素をビジュアルツリーから再帰的に検索
    /// </summary>
    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0, n = VisualTreeHelper.GetChildrenCount(root); i < n; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T t) return t;
            var r = FindDescendant<T>(child);
            if (r != null) return r;
        }
        return null;
    }

    /// <summary>
    /// TextBoxがキーボードフォーカスを得たときの処理
    /// LostKeyboardFocusイベントを購読
    /// </summary>
    private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (_textBox == null) return;
        _textBox.GotKeyboardFocus -= TextBox_GotKeyboardFocus;
        _textBox.LostKeyboardFocus += TextBox_LostKeyboardFocus;
    }

    /// <summary>
    /// TextBoxがキーボードフォーカスを失ったときの処理
    /// GotKeyboardFocusイベントを再購読し、AddOnFocusLostが有効ならトークン追加
    /// </summary>
    private void TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (_textBox == null) return;
        _textBox.LostKeyboardFocus -= TextBox_LostKeyboardFocus;
        _textBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
        if (AddOnFocusLost)
        {
            AddToken();
        }
    }

    /// <summary>
    /// TextBoxのPreviewKeyDownイベントハンドラ
    /// Backキーでカーソルが先頭かつトークンが存在する場合は直前トークンにフォーカス
    /// Enter/Tabキーでトークン追加
    /// </summary>
    private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_textBox == null) return;
        int currentCursorPosition = _textBox.SelectionStart;
        var isEmpty = String.IsNullOrWhiteSpace(_textBox.Text);
        switch (e.Key)
        {
            case Key.Back when currentCursorPosition == 0 && _textBox.SelectionLength == 0 && Items.Count > 0:
                e.Handled = true;
                var container = ItemContainerGenerator.ContainerFromIndex(Items.Count - 1);
                if (container is IInputElement element)
                {
                    Keyboard.Focus(element);
                }
                break;

            case Key.Enter when AcceptsReturn && !isEmpty:
            case Key.Tab when AcceptsTab && !isEmpty:
                e.Handled = true;
                AddToken();
                break;
        }
    }

    /// <summary>
    /// TextBoxのTextChangedイベントハンドラ
    /// 区切り文字が入力された場合、テキストを分割してトークン追加
    /// </summary>
    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string t = _textBox != null ? _textBox.Text : String.Empty;

        if (!String.IsNullOrEmpty(TokenDelimiter) && t.Contains(TokenDelimiter))
        {
            bool lastDelimited = t[t.Length - 1] == TokenDelimiter[0];

            string[] tokens = t.Split(new[] { TokenDelimiter }, StringSplitOptions.RemoveEmptyEntries);
            int numberToProcess = lastDelimited ? tokens.Length : tokens.Length - 1;
            for (int position = 0; position < numberToProcess; position++)
            {
                AddToken(tokens[position]);
            }

            if (_textBox == null) return;
            if (lastDelimited)
            {
                _textBox.Text = String.Empty;
                //_wrapPanel.InvalidateMeasure();
            }
            else
            {
                _textBox.Text = tokens[tokens.Length - 1];
                _textBox.CaretIndex = _textBox.Text.Length;
            }
        }
    }
}
