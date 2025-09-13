using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TokenizingTextBox;

/// <summary>
/// WrapPanelは、子要素を自動的に折り返して配置するWPF用のカスタムパネルです。
/// 水平方向または垂直方向に子要素を並べ、パネルの端に達すると次の行（または列）に折り返します。
/// 各種間隔やパディング、子要素のストレッチ方法を柔軟に指定できます。
/// </summary>
public partial class WrapPanel : Panel
{
    /// <summary>
    /// <see cref="HorizontalSpacing"/> 依存関係プロパティを識別します。
    /// 子要素間の水平方向の間隔（ピクセル単位）を指定します。
    /// </summary>
    public static readonly DependencyProperty HorizontalSpacingProperty =
        DependencyProperty.Register(
            nameof(HorizontalSpacing),
            typeof(double),
            typeof(WrapPanel),
            new PropertyMetadata(0d, LayoutPropertyChanged));

    /// <summary>
    /// <see cref="Orientation"/> 依存関係プロパティを識別します。
    /// 子要素の配置方向（Horizontal/Vertical）を指定します。
    /// </summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(WrapPanel),
            new PropertyMetadata(Orientation.Horizontal, LayoutPropertyChanged));

    /// <summary>
    /// Padding 依存関係プロパティを識別します。
    /// パネルの枠と子要素との間隔（余白）を指定します。
    /// </summary>
    /// <returns><see cref="Padding"/> 依存関係プロパティの識別子。</returns>
    public static readonly DependencyProperty PaddingProperty =
        DependencyProperty.Register(
            nameof(Padding),
            typeof(Thickness),
            typeof(WrapPanel),
            new PropertyMetadata(default(Thickness), LayoutPropertyChanged));

    /// <summary>
    /// <see cref="StretchChild"/> 依存関係プロパティを識別します。
    /// 子要素のストレッチ方法（特に最後の要素の伸縮）を指定します。
    /// </summary>
    /// <returns><see cref="StretchChild"/> 依存関係プロパティの識別子。</returns>
    public static readonly DependencyProperty StretchChildProperty =
        DependencyProperty.Register(
            nameof(StretchChild),
            typeof(StretchChild),
            typeof(WrapPanel),
            new PropertyMetadata(StretchChild.None, LayoutPropertyChanged));

    /// <summary>
    /// <see cref="VerticalSpacing"/> 依存関係プロパティを識別します。
    /// 子要素間の垂直方向の間隔（ピクセル単位）を指定します。
    /// </summary>
    public static readonly DependencyProperty VerticalSpacingProperty =
        DependencyProperty.Register(
            nameof(VerticalSpacing),
            typeof(double),
            typeof(WrapPanel),
            new PropertyMetadata(0d, LayoutPropertyChanged));

    /// <summary>
    /// 子要素間の水平方向の間隔（ピクセル単位）を取得または設定します。
    /// OrientationがHorizontalの場合は行内の間隔、Verticalの場合は列間の間隔となります。
    /// </summary>
    public double HorizontalSpacing
    {
        get { return (double)GetValue(HorizontalSpacingProperty); }
        set { SetValue(HorizontalSpacingProperty, value); }
    }

    /// <summary>
    /// WrapPanelの子要素配置方向を取得または設定します。
    /// Horizontal: 横並びでパネル幅に達すると折り返し
    /// Vertical: 縦並びでパネル高さに達すると折り返し
    /// </summary>
    public Orientation Orientation
    {
        get { return (Orientation)GetValue(OrientationProperty); }
        set { SetValue(OrientationProperty, value); }
    }

    /// <summary>
    /// パネルの枠と子要素との間隔（余白）を取得または設定します。
    /// Thickness構造体で上下左右の余白を指定します。
    /// </summary>
    /// <returns>
    /// 枠と子要素間の余白（Thickness値）。
    /// </returns>
    public Thickness Padding
    {
        get { return (Thickness)GetValue(PaddingProperty); }
        set { SetValue(PaddingProperty, value); }
    }

    /// <summary>
    /// 子要素のストレッチ方法（特に最後の要素の伸縮）を取得または設定します。
    /// StretchChild.None: 伸縮なし
    /// StretchChild.Last: 最後の子要素を残りスペースに合わせて伸縮
    /// </summary>
    public StretchChild StretchChild
    {
        get { return (StretchChild)GetValue(StretchChildProperty); }
        set { SetValue(StretchChildProperty, value); }
    }

    /// <summary>
    /// 子要素間の垂直方向の間隔（ピクセル単位）を取得または設定します。
    /// OrientationがVerticalの場合は列内の間隔、Horizontalの場合は行間の間隔となります。
    /// </summary>
    public double VerticalSpacing
    {
        get { return (double)GetValue(VerticalSpacingProperty); }
        set { SetValue(VerticalSpacingProperty, value); }
    }

    /// <inheritdoc />
    /// <summary>
    /// 子要素の配置（Arrange）を行います。
    /// パネルのサイズ内で子要素を並べ、端に達したら折り返して次の行（または列）に配置します。
    /// StretchChildプロパティにより、最後の子要素を残りスペースに合わせて伸縮することも可能です。
    /// ネストされたPanel（テンプレート内のPanel）も再帰的に配置します。
    /// </summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count > 0)
        {
            var parentMeasure = new UvMeasure(Orientation, finalSize.Width, finalSize.Height);
            var spacingMeasure = new UvMeasure(Orientation, HorizontalSpacing, VerticalSpacing);
            var paddingStart = new UvMeasure(Orientation, Padding.Left, Padding.Top);
            var paddingEnd = new UvMeasure(Orientation, Padding.Right, Padding.Bottom);
            var position = new UvMeasure(Orientation, Padding.Left, Padding.Top);

            double currentV = 0;
            // 子要素を配置するローカル関数
            void arrange(UIElement child, bool isLast = false)
            {
                // FrameworkElementの場合、テンプレート内のPanelを再帰的に配置
                if (child is FrameworkElement fe)
                {
                    var nestedPanel = TryGetTemplateRootPanel(fe);
                    if (nestedPanel != null && nestedPanel.Children.Count > 0)
                    {
                        var nestedIndex = nestedPanel.Children.Count;
                        for (var i = 0; i < nestedIndex; i++)
                        {
                            arrange(nestedPanel.Children[i], isLast && (nestedIndex - i) == 1);
                        }
                        return;
                    }
                }

                var desiredMeasure = new UvMeasure(Orientation, child.DesiredSize.Width, child.DesiredSize.Height);
                if (desiredMeasure.U == 0)
                {
                    return; // 非表示（Collapsed）の場合は間隔を追加しない
                }

                if ((desiredMeasure.U + position.U + paddingEnd.U) > parentMeasure.U)
                {
                    // 次の行（または列）へ折り返し
                    position.U = paddingStart.U;
                    position.V += currentV + spacingMeasure.V;
                    currentV = 0;
                }

                // StretchChild.Lastの場合、最後の子要素を残りスペースに合わせて伸縮
                if (isLast && StretchChild == StretchChild.Last)
                {
                    desiredMeasure.U = parentMeasure.U - position.U;
                }

                // 子要素を配置
                if (Orientation == Orientation.Horizontal)
                {
                    child.Arrange(new Rect(position.U, position.V, desiredMeasure.U, desiredMeasure.V));
                }
                else
                {
                    child.Arrange(new Rect(position.V, position.U, desiredMeasure.V, desiredMeasure.U));
                }

                // 次の子要素の配置位置を更新
                position.U += desiredMeasure.U + spacingMeasure.U;
                currentV = Math.Max(desiredMeasure.V, currentV);
            }

            var lastIndex = Children.Count;
            for (var i = 0; i < lastIndex; i++)
            {
                arrange(Children[i], (lastIndex - i) == 1);
            }
        }

        return finalSize;
    }

    /// <summary>
    /// 指定された FrameworkElement のテンプレート内から Panel を探索して取得します。
    /// WPF の標準的なパターンに従い、GetTemplateChild や FindName を使用して
    /// テンプレート内の Panel 要素を取得します。
    /// </summary>
    /// <param name="element">探索対象の要素。</param>
    /// <returns>テンプレート内に見つかった Panel。見つからない場合は null。</returns>
    private static Panel? TryGetTemplateRootPanel(FrameworkElement element)
    {
        // Controlの場合はテンプレートを適用
        if (element is Control control)
        {
            control.ApplyTemplate();
            
            // 標準的なWPFパターン: 名前付きテンプレート部品を探す
            // TokenizingTextBoxItemの場合、TokenInnerGridは内部実装の詳細なのでスキップ
            if (control.Template?.FindName("PART_Panel", control) is Panel namedPanel)
            {
                return namedPanel;
            }
        }

        // フォールバック: ビジュアルツリーを再帰的に探索
        return FindDescendantPanel(element);
    }

    /// <summary>
    /// 指定された要素の子孫から最初の Panel を再帰的に検索します。
    /// TokenizingTextBoxItem 固有の TokenInnerGrid は内部実装詳細として除外します。
    /// </summary>
    /// <param name="parent">検索を開始する親要素。</param>
    /// <returns>見つかった Panel。見つからない場合は null。</returns>
    private static Panel? FindDescendantPanel(DependencyObject parent)
    {
        if (parent == null) return null;

        for (int i = 0, count = VisualTreeHelper.GetChildrenCount(parent); i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            // Panel が見つかった場合
            if (child is Panel panel)
            {
                // TokenInnerGrid は TokenizingTextBoxItem の内部実装詳細なのでスキップ
                if (panel.Name != "TokenInnerGrid")
                {
                    return panel;
                }
            }

            // 再帰的に子要素を探索
            var descendantPanel = FindDescendantPanel(child);
            if (descendantPanel != null)
            {
                return descendantPanel;
            }
        }

        return null;
    }
    /// <inheritdoc />
    /// <summary>
    /// 子要素のサイズ計測（Measure）を行います。
    /// パネルの利用可能サイズ内で子要素を並べ、折り返し位置や必要なサイズを計算します。
    /// ネストされたPanel（テンプレート内のPanel）も再帰的に計測します。
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        // パディング分を利用可能サイズから減算
        availableSize.Width = availableSize.Width - Padding.Left - Padding.Right;
        availableSize.Height = availableSize.Height - Padding.Top - Padding.Bottom;
        var totalMeasure = UvMeasure.s_zero;
        var parentMeasure = new UvMeasure(Orientation, availableSize.Width, availableSize.Height);
        var spacingMeasure = new UvMeasure(Orientation, HorizontalSpacing, VerticalSpacing);
        var lineMeasure = UvMeasure.s_zero;

        // 子要素コレクションを計測するローカル関数
        void measure(UIElementCollection elementCollection)
        {
            foreach (UIElement child in elementCollection)
            {
                // FrameworkElementの場合、テンプレート内のPanelを再帰的に計測
                if (child is FrameworkElement fe)
                {
                    var nestedPanel = TryGetTemplateRootPanel(fe);
                    if (nestedPanel != null)
                    {
                        measure(nestedPanel.Children);
                        continue;
                    }
                }

                child.Measure(availableSize);
                var currentMeasure = new UvMeasure(Orientation, child.DesiredSize.Width, child.DesiredSize.Height);
                if (currentMeasure.U == 0)
                {
                    continue; // 非表示（Collapsed）の場合は無視
                }

                // 最初の要素には間隔を追加しない
                double uChange = lineMeasure.U == 0
                    ? currentMeasure.U
                    : currentMeasure.U + spacingMeasure.U;
                if (parentMeasure.U >= uChange + lineMeasure.U)
                {
                    lineMeasure.U += uChange;
                    lineMeasure.V = Math.Max(lineMeasure.V, currentMeasure.V);
                }
                else
                {
                    // 折り返し（新しい行または列）
                    totalMeasure.U = Math.Max(lineMeasure.U, totalMeasure.U);
                    totalMeasure.V += lineMeasure.V + spacingMeasure.V;

                    // 新しい行（列）に要素を追加
                    if (parentMeasure.U > currentMeasure.U)
                    {
                        lineMeasure = currentMeasure;
                    }
                    // 要素が1行（列）を占有する場合
                    else
                    {
                        totalMeasure.U = Math.Max(currentMeasure.U, totalMeasure.U);
                        totalMeasure.V += currentMeasure.V;
                        lineMeasure = UvMeasure.s_zero;
                    }
                }
            }
        }
        measure(Children);

        // 最後の行（列）のサイズを合計に反映
        totalMeasure.U = Math.Max(lineMeasure.U, totalMeasure.U);
        totalMeasure.V += lineMeasure.V;

        totalMeasure.U = Math.Ceiling(totalMeasure.U);

        // 配置方向に応じてサイズを返す
        return Orientation == Orientation.Horizontal ? new Size(totalMeasure.U, totalMeasure.V) : new Size(totalMeasure.V, totalMeasure.U);
    }

    /// <summary>
    /// レイアウト関連プロパティが変更された際に、Measure/Arrangeを再実行します。
    /// </summary>
    private static void LayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WrapPanel wp)
        {
            wp.InvalidateMeasure();
            wp.InvalidateArrange();
        }
    }
}
