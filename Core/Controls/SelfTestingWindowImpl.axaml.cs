using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CarinaStudio.AppSuite.Testing;
using CarinaStudio.Threading;
using CarinaStudio.Windows.Input;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Window to perform self testing.
/// </summary>
class SelfTestingWindowImpl : Window<IAppSuiteApplication>
{
    /// <summary>
    /// Converter to convert from <see cref="TestCaseState"/> to <see cref="IImage"/>.
    /// </summary>
    public static readonly IValueConverter TestCaseIconConverter = new FuncValueConverter<TestCaseState, IImage?>(state =>
    {
        var app = AppSuiteApplication.CurrentOrNull;
        if (app == null)
            return null;
        return state switch
        {
            TestCaseState.Cancelling => app.FindResourceOrDefault<IImage>("Image/Icon.StopMedia"),
            TestCaseState.Failed => app.FindResourceOrDefault<IImage>("Image/Icon.Error.Colored"),
            TestCaseState.Running
            or TestCaseState.SettingUp
            or TestCaseState.TearingDown => app.FindResourceOrDefault<IImage>("Image/Icon.PlayMedia"),
            TestCaseState.Succeeded => app.FindResourceOrDefault<IImage>("Image/Icon.Success.Colored"),
            TestCaseState.WaitingForRunning => app.FindResourceOrDefault<IImage>("Image/Icon.Waiting"),
            _ => app.FindResourceOrDefault<IImage>("Image/Icon.Lab"),
        };
    });


    // Dummy test case.
    class DummyTestCase : TestCase
    {
        // Constructor.
        public DummyTestCase(IAppSuiteApplication app) : base(app, null, "Dummy")
        { }

        /// <inheritdoc/>
        protected override async Task OnRunAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(3000, cancellationToken);
        }
    }


    // Fields.
    readonly TestManager testManager = TestManager.Default;
    readonly TreeView testCasesTreeView;


    // Constructor.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TestCase))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TestCaseCategory))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TestManager))]
    [DynamicDependency(nameof(CancelAllTestCases))]
    [DynamicDependency(nameof(CancelTestCaseCommand))]
    [DynamicDependency(nameof(RunAllTestCases))]
    [DynamicDependency(nameof(RunTestCaseCommand))]
    [DynamicDependency(nameof(RunTestCasesInCategoryCommand))]
    public SelfTestingWindowImpl()
    {
        //this.testManager.AddTestCase(typeof(DummyTestCase));
        this.CancelTestCaseCommand = new Command<TreeViewItem>(this.CancelTestCase);
        this.RunTestCaseCommand = new Command<TreeViewItem>(this.RunTestCase);
        this.RunTestCasesInCategoryCommand = new Command<TreeViewItem>(this.RunTestCasesInCategory);
        AvaloniaXamlLoader.Load(this);
        this.testCasesTreeView = this.Get<TreeView>(nameof(testCasesTreeView)).Also(it =>
        {
            it.ItemsSource = this.testManager.TestCaseCategories;
        });
        this.Title = this.Application.GetFormattedString("SelfTestingWindow.Title", this.Application.Name);
    }


    /// <summary>
    /// Cancel all running and waiting test cases.
    /// </summary>
    public void CancelAllTestCases() =>
        this.testManager.CancelAllTestCases();
    

    // Cancel the test case.
    void CancelTestCase(TreeViewItem item)
    {
        if (item.DataContext is TestCase testCase)
            this.testManager.CancelTestCase(testCase);
    }


    /// <summary>
    /// Command to cancel the test case.
    /// </summary>
    public ICommand CancelTestCaseCommand { get; }


    /// <inheritdoc/>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        this.testManager.CancelAllTestCases();
        if (this.testManager.IsRunningTestCases)
        {
            _ = new MessageDialog()
            {
                Icon = MessageDialogIcon.Warning,
                Message = this.Application.GetObservableString("SelfTestingWindow.NeedToWaitForRunningTestCases"),
            }.ShowDialog(this);
            e.Cancel = true;
        }
        base.OnClosing(e);
    }


    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        this.SynchronizationContext.Post(() => this.testCasesTreeView.Focus());
    }


    /// <inheritdoc/>
    protected override WindowTransparencyLevel OnSelectTransparentLevelHint() =>
        WindowTransparencyLevel.None;
    

    /// <summary>
    /// Run all test cases.
    /// </summary>
    public void RunAllTestCases() =>
        this.testManager.RunAllTestCases();
    

    // Run the test case.
    void RunTestCase(TreeViewItem item)
    {
        if (item.DataContext is TestCase testCase)
            this.testManager.RunTestCase(testCase);
    }


    /// <summary>
    /// Command to run the test case.
    /// </summary>
    public ICommand RunTestCaseCommand { get; }


    // Run the test cases in specific category.
    void RunTestCasesInCategory(TreeViewItem item)
    {
        if (item.DataContext is TestCaseCategory category)
            this.testManager.RunTestCases(category);
    }


    /// <summary>
    /// Command to run the test cases in specific category.
    /// </summary>
    public ICommand RunTestCasesInCategoryCommand { get; }
}
