using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Charts;

namespace FluentDocs.Abstractions.Builders.Impl;

internal sealed class ChartBuilder : IChartBuilder
{
    private ChartType _type = ChartType.Bar;
    private string? _title;
    private readonly List<ChartDataSeries> _series = [];
    private ChartAxisDefinition? _xAxis;
    private ChartAxisDefinition? _yAxis;
    private ChartLegend? _legend;
    private ChartStyle? _style;
    private int _widthPx = 800;
    private int _heightPx = 600;

    public IChartBuilder OfType(ChartType type)
    {
        _type = type;
        return this;
    }

    public IChartBuilder Bar()
    {
        return OfType(ChartType.Bar);
    }

    public IChartBuilder Column()
    {
        return OfType(ChartType.Column);
    }

    public IChartBuilder Line()
    {
        return OfType(ChartType.Line);
    }

    public IChartBuilder Area()
    {
        return OfType(ChartType.Area);
    }

    public IChartBuilder Pie()
    {
        return OfType(ChartType.Pie);
    }

    public IChartBuilder Doughnut()
    {
        return OfType(ChartType.Doughnut);
    }

    public IChartBuilder Scatter()
    {
        return OfType(ChartType.Scatter);
    }

    public IChartBuilder Radar()
    {
        return OfType(ChartType.Radar);
    }

    public IChartBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public IChartBuilder AddSeries(Action<IChartSeriesBuilder> configure)
    {
        var builder = new ChartSeriesBuilder();
        configure(builder);
        _series.Add(builder.Build());
        return this;
    }

    public IChartBuilder WithXAxis(Action<IChartAxisBuilder> configure)
    {
        var builder = new ChartAxisBuilder();
        configure(builder);
        _xAxis = builder.Build();
        return this;
    }

    public IChartBuilder WithYAxis(Action<IChartAxisBuilder> configure)
    {
        var builder = new ChartAxisBuilder();
        configure(builder);
        _yAxis = builder.Build();
        return this;
    }

    public IChartBuilder WithLegend(Action<IChartLegendBuilder> configure)
    {
        var builder = new ChartLegendBuilder();
        configure(builder);
        _legend = builder.Build();
        return this;
    }

    public IChartBuilder WithStyle(Action<IChartStyleBuilder> configure)
    {
        var builder = new ChartStyleBuilder();
        configure(builder);
        _style = builder.Build();
        return this;
    }

    public IChartBuilder WithSize(int widthPx, int heightPx)
    {
        _widthPx = widthPx;
        _heightPx = heightPx;
        return this;
    }

    internal ChartDefinition Build()
    {
        return new ChartDefinition
        {
            Type = _type,
            Title = _title,
            Series = _series,
            XAxis = _xAxis,
            YAxis = _yAxis,
            Legend = _legend,
            Style = _style,
            WidthPx = _widthPx,
            HeightPx = _heightPx
        };
    }
}

internal sealed class ChartSeriesBuilder : IChartSeriesBuilder
{
    private string? _label;
    private readonly List<string> _categories = [];
    private readonly List<double> _values = [];
    private string? _color;

    public IChartSeriesBuilder WithLabel(string label)
    {
        _label = label;
        return this;
    }

    public IChartSeriesBuilder AddCategory(string category)
    {
        _categories.Add(category);
        return this;
    }

    public IChartSeriesBuilder WithCategories(params string[] categories)
    {
        _categories.AddRange(categories);
        return this;
    }

    public IChartSeriesBuilder AddValue(double value)
    {
        _values.Add(value);
        return this;
    }

    public IChartSeriesBuilder WithValues(params double[] values)
    {
        _values.AddRange(values);
        return this;
    }

    public IChartSeriesBuilder WithColor(string hex)
    {
        _color = hex;
        return this;
    }

    internal ChartDataSeries Build()
    {
        return new ChartDataSeries
        {
            Label = _label,
            Categories = _categories,
            Values = _values,
            Color = _color
        };
    }
}

internal sealed class ChartAxisBuilder : IChartAxisBuilder
{
    private string? _title;
    private double? _min;
    private double? _max;
    private bool _gridLines = true;
    private string? _labelFormat;

    public IChartAxisBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public IChartAxisBuilder WithMin(double min)
    {
        _min = min;
        return this;
    }

    public IChartAxisBuilder WithMax(double max)
    {
        _max = max;
        return this;
    }

    public IChartAxisBuilder ShowGridLines(bool show)
    {
        _gridLines = show;
        return this;
    }

    public IChartAxisBuilder WithLabelFormat(string format)
    {
        _labelFormat = format;
        return this;
    }

    internal ChartAxisDefinition Build()
    {
        return new ChartAxisDefinition
        {
            Title = _title,
            Min = _min,
            Max = _max,
            GridLines = _gridLines,
            LabelFormat = _labelFormat
        };
    }
}

internal sealed class ChartLegendBuilder : IChartLegendBuilder
{
    private bool _visible = true;
    private LegendPosition _position = LegendPosition.Bottom;

    public IChartLegendBuilder Show(bool visible)
    {
        _visible = visible;
        return this;
    }

    public IChartLegendBuilder AtPosition(LegendPosition position)
    {
        _position = position;
        return this;
    }

    internal ChartLegend Build()
    {
        return new ChartLegend
        {
            Visible = _visible,
            Position = _position
        };
    }
}

internal sealed class ChartStyleBuilder : IChartStyleBuilder
{
    private string? _bgColor;
    private string? _fontFamily;
    private double? _fontSize;
    private readonly List<string> _palette = [];

    public IChartStyleBuilder WithBackgroundColor(string hex)
    {
        _bgColor = hex;
        return this;
    }

    public IChartStyleBuilder WithFont(string fontFamily, double? sizePt)
    {
        _fontFamily = fontFamily;
        _fontSize = sizePt;
        return this;
    }

    public IChartStyleBuilder WithPalette(params string[] hexColors)
    {
        _palette.AddRange(hexColors);
        return this;
    }

    internal ChartStyle Build()
    {
        return new ChartStyle
        {
            BackgroundColor = _bgColor,
            FontFamily = _fontFamily,
            FontSizePt = _fontSize,
            Palette = _palette
        };
    }
}
