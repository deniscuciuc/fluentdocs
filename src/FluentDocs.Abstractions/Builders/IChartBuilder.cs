using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Charts;

namespace FluentDocs.Abstractions.Builders;

/// <summary>
/// Fluent builder for constructing a <see cref="ChartDefinition"/>.
/// </summary>
public interface IChartBuilder
{
    IChartBuilder OfType(ChartType type);
    IChartBuilder Bar();
    IChartBuilder Column();
    IChartBuilder Line();
    IChartBuilder Area();
    IChartBuilder Pie();
    IChartBuilder Doughnut();
    IChartBuilder Scatter();
    IChartBuilder Radar();
    IChartBuilder WithTitle(string title);
    IChartBuilder AddSeries(Action<IChartSeriesBuilder> configure);
    IChartBuilder WithXAxis(Action<IChartAxisBuilder> configure);
    IChartBuilder WithYAxis(Action<IChartAxisBuilder> configure);
    IChartBuilder WithLegend(Action<IChartLegendBuilder> configure);
    IChartBuilder WithStyle(Action<IChartStyleBuilder> configure);
    IChartBuilder WithSize(int widthPx, int heightPx);
}

/// <summary>
/// Builder for a single chart data series.
/// </summary>
public interface IChartSeriesBuilder
{
    IChartSeriesBuilder WithLabel(string label);
    IChartSeriesBuilder AddCategory(string category);
    IChartSeriesBuilder WithCategories(params string[] categories);
    IChartSeriesBuilder AddValue(double value);
    IChartSeriesBuilder WithValues(params double[] values);
    IChartSeriesBuilder WithColor(string hex);
}

/// <summary>
/// Builder for a chart axis.
/// </summary>
public interface IChartAxisBuilder
{
    IChartAxisBuilder WithTitle(string title);
    IChartAxisBuilder WithMin(double min);
    IChartAxisBuilder WithMax(double max);
    IChartAxisBuilder ShowGridLines(bool show = true);
    IChartAxisBuilder WithLabelFormat(string format);
}

/// <summary>
/// Builder for chart legend configuration.
/// </summary>
public interface IChartLegendBuilder
{
    IChartLegendBuilder Show(bool visible = true);
    IChartLegendBuilder AtPosition(LegendPosition position);
}

/// <summary>
/// Builder for chart visual styling.
/// </summary>
public interface IChartStyleBuilder
{
    IChartStyleBuilder WithBackgroundColor(string hex);
    IChartStyleBuilder WithFont(string fontFamily, double? sizePt = null);
    IChartStyleBuilder WithPalette(params string[] hexColors);
}
