using Avalonia;
using Avalonia.Media;
using CarinaStudio.Configuration;
using CarinaStudio.Threading;
using System;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Notification.
/// </summary>
public class Notification : AvaloniaObject
{
    /// <summary>
    /// Property of <see cref="Icon"/>.
    /// </summary>
    public static readonly StyledProperty<IList<NotificationAction>?> ActionsProperty = AvaloniaProperty.Register<Notification, IList<NotificationAction>?>(nameof(Actions));
    /// <summary>
    /// Property of <see cref="Icon"/>.
    /// </summary>
    public static readonly StyledProperty<IImage?> IconProperty = AvaloniaProperty.Register<Notification, IImage?>(nameof(Icon));
    /// <summary>
    /// Property of <see cref="IsDismissing"/>.
    /// </summary>
    public static readonly DirectProperty<Notification, bool> IsDismissingProperty = AvaloniaProperty.RegisterDirect<Notification, bool>(nameof(IsDismissing), n => n.isDismissing);
    /// <summary>
    /// Property of <see cref="IsShowing"/>.
    /// </summary>
    public static readonly DirectProperty<Notification, bool> IsShowingProperty = AvaloniaProperty.RegisterDirect<Notification, bool>(nameof(IsShowing), n => n.isShowing);
    /// <summary>
    /// Property of <see cref="IsVisible"/>.
    /// </summary>
    public static readonly DirectProperty<Notification, bool> IsVisibleProperty = AvaloniaProperty.RegisterDirect<Notification, bool>(nameof(IsVisible), n => n.isVisible);
    /// <summary>
    /// Property of <see cref="Message"/>.
    /// </summary>
    public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<Notification, string?>(nameof(Message));
    /// <summary>
    /// Property of <see cref="Presenter"/>.
    /// </summary>
    public static readonly DirectProperty<Notification, NotificationPresenter?> PresenterProperty = AvaloniaProperty.RegisterDirect<Notification, NotificationPresenter?>(nameof(Presenter), n => n.presenter);
    /// <summary>
    /// Property of <see cref="Timeout"/>.
    /// </summary>
    public static readonly StyledProperty<TimeSpan?> TimeoutProperty = AvaloniaProperty.Register<Notification, TimeSpan?>(nameof(Timeout), TimeSpan.FromSeconds(5));
    /// <summary>
    /// Property of <see cref="Title"/>.
    /// </summary>
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<Notification, string?>(nameof(Title));


    // Fields.
    readonly ScheduledAction completeShowingAction;
    readonly ScheduledAction dismissAction;
    bool isDismissing;
    bool isShowing;
    bool isVisible;
    NotificationPresenter? presenter;


    /// <summary>
    /// Initialize new <see cref="Notification"/> instance.
    /// </summary>
    public Notification()
    {
        this.completeShowingAction = new(() =>
            this.SetAndRaise(IsShowingProperty, ref this.isShowing, false));
        this.dismissAction = new(this.Dismiss);
        IAppSuiteApplication.CurrentOrNull?.Configuration.GetValueOrDefault(ConfigurationKeys.DefaultTimeoutOfNotification).Let(timeout =>
            this.SetValue(TimeoutProperty, TimeSpan.FromMilliseconds(timeout)));
    }


    /// <summary>
    /// Get or set list of action of notification.
    /// </summary>
    public IList<NotificationAction>? Actions
    {
        get => this.GetValue(ActionsProperty);
        set => this.SetValue(ActionsProperty, value);
    }


    /// <summary>
    /// Dismiss the notification.
    /// </summary>
    public void Dismiss()
    {
        if (this.isDismissing)
            return;
        this.completeShowingAction.Cancel();
        this.dismissAction.Cancel();
        this.SetAndRaise(IsShowingProperty, ref this.isShowing, false);
        this.SetAndRaise(IsDismissingProperty, ref this.isDismissing, true);
        this.presenter?.DismissNotification(this);
    }


    /// <summary>
    /// Raised when notification has been dismissed.
    /// </summary>
    public event EventHandler? Dismissed;


    /// <summary>
    /// Get or set icon of notification.
    /// </summary>
    public IImage? Icon
    {
        get => this.GetValue(IconProperty);
        set => this.SetValue(IconProperty, value);
    }
    
    
    /// <summary>
    /// Get whether notification is dismissing or not.
    /// </summary>
    public bool IsDismissing => this.isDismissing;
    
    
    /// <summary>
    /// Get whether notification is showing or not.
    /// </summary>
    public bool IsShowing => this.isShowing;


    /// <summary>
    /// Get whether notification is visible to user or not.
    /// </summary>
    public bool IsVisible => this.isVisible;
    
    
    /// <summary>
    /// Get or set message of notification.
    /// </summary>
    public string? Message
    {
        get => this.GetValue(MessageProperty);
        set => this.SetValue(MessageProperty, value);
    }


    // Called when dismissed.
    internal void OnDismissed()
    {
        if (this.presenter is null)
            return;
        this.SetAndRaise(PresenterProperty, ref this.presenter, null);
        this.SetAndRaise(IsDismissingProperty, ref this.isDismissing, false);
        this.SetAndRaise(IsShowingProperty, ref this.isShowing, false);
        this.SetAndRaise(IsVisibleProperty, ref this.isVisible, false);
        this.Dismissed?.Invoke(this, EventArgs.Empty);
    }
    
    
    // Called when being shown.
    internal void OnShowing(NotificationPresenter presenter)
    {
        if (this.presenter is not null)
        {
            if (this.presenter == presenter)
                return;
            throw new InternalStateCorruptedException("Notification has been presented by another presenter.");
        }
        this.SetAndRaise(PresenterProperty, ref this.presenter, presenter);
        this.SetAndRaise(IsVisibleProperty, ref this.isVisible, true);
        this.SetAndRaise(IsShowingProperty, ref this.isShowing, true);
        this.completeShowingAction.Schedule(IAppSuiteApplication.Current.FindResourceOrDefault("TimeSpan/NotificationPresenter.Notification", TimeSpan.FromMilliseconds(500)));
        this.GetValue(TimeoutProperty)?.Let(it => this.dismissAction.Schedule(it));
    }


    /// <summary>
    /// Get <see cref="NotificationPresenter"/> which is currently presenting the notification.
    /// </summary>
    public NotificationPresenter? Presenter => this.presenter;
    
    
    // Start auto dismiss of notification.
    internal void StartAutoDismiss()
    {
        if (this.isVisible)
            this.GetValue(TimeoutProperty)?.Let(it => this.dismissAction.Reschedule(it));
    }


    // Stop auto dismiss of notification.
    internal void StopAutoDismiss() =>
        this.dismissAction.Cancel();


    /// <summary>
    /// Get or set timeout to dismiss notification.
    /// </summary>
    public TimeSpan? Timeout
    {
        get => this.GetValue(TimeoutProperty);
        set => this.SetValue(TimeoutProperty, value);
    }
    
    
    /// <summary>
    /// Get or set title of notification.
    /// </summary>
    public string? Title
    {
        get => this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }
}