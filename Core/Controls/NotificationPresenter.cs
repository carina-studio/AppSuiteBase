using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using CarinaStudio.Collections;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.VisualTree;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Implementation of <see cref="INotificationPresenter"/>.
/// </summary>
[TemplatePart("PART_NotificationsPanel", typeof(Panel))]
public class NotificationPresenter : TemplatedControl, INotificationPresenter
{
    /// <summary>
    /// Define <see cref="HorizontalNotificationsAlignment"/> property.
    /// </summary>
    public static readonly AvaloniaProperty<HorizontalAlignment> HorizontalNotificationsAlignmentProperty = AvaloniaProperty.Register<NotificationPresenter, HorizontalAlignment>(nameof(HorizontalNotificationsAlignment), HorizontalAlignment.Right);
    /// <summary>
    /// Define <see cref="VerticalNotificationsAlignment"/> property.
    /// </summary>
    public static readonly AvaloniaProperty<VerticalAlignment> VerticalNotificationsAlignmentProperty = AvaloniaProperty.Register<NotificationPresenter, VerticalAlignment>(nameof(VerticalNotificationsAlignment), VerticalAlignment.Bottom);
    
    
    // Fields.
    readonly Dictionary<Notification, Panel> notificationActionsPanels = new();
    readonly Dictionary<Notification, Control> notificationControls = new();
    DataTemplate? notificationActionControlTemplate;
    DataTemplate? notificationControlTemplate;
    readonly HashSet<Notification> dismissingNotifications = new();
    readonly ObservableList<Notification> notifications = new();
    Panel? notificationsPanel;


    /// <summary>
    /// Initialize new <see cref="NotificationPresenter"/> instance.
    /// </summary>
    public NotificationPresenter()
    {
        this.SynchronizationContext = IAppSuiteApplication.Current.SynchronizationContext;
    }
    
    
    /// <inheritdoc/>
    public void AddNotification(Notification notification)
    {
        // check state
        this.VerifyAccess();
        if (notification.Presenter is NotificationPresenter currentPresenter)
        {
            if (currentPresenter == this)
                return;
            throw new InvalidOperationException("Notification has been added to another presenter.");
        }

        // create view
        this.BuildNotificationControl(notification)?.Let(control =>
        {
            this.notificationControls[notification] = control;
            this.notificationsPanel?.Children.Add(control);
            control.Opacity = 1;
            (control.RenderTransform as TranslateTransform)?.Let(it =>
            {
                it.X = 0;
                it.Y = 0;
            });
        });
        
        // attach to notification
        notification.PropertyChanged += this.OnNotificationPropertyChanged;

        // add and show
        this.notifications.Add(notification);
        notification.OnShowing(this);
    }
    
    
    // Build controls for actions of notification.
    void BuildNotificationActionControls(Notification notification, Panel actionsPanel)
    {
        var actions = notification.Actions;
        if (actions.IsNullOrEmpty())
            return;
        var template = this.notificationActionControlTemplate;
        if (template is not null)
        {
            foreach (var action in actions)
            {
                var control = template.Build(action)?.Also(it => it.DataContext = action);
                if (control is not null)
                    actionsPanel.Children.Add(control);
            }
        }
    }
    
    
    // Build view for notification.
    Control? BuildNotificationControl(Notification notification)
    {
        return this.notificationControlTemplate?.Build(notification)?.Also(it =>
        {
            it.DataContext = notification;
            it.FindDescendantOfTypeAndName<Panel>("PART_ActionsPanel")?.Let(actionsPanel =>
            {
                this.notificationActionsPanels[notification] = actionsPanel;
                this.BuildNotificationActionControls(notification, actionsPanel);
            });
            it.GetObservable(IsPointerOverProperty).Subscribe(isPointerOver =>
            {
                if (isPointerOver)
                    notification.StopAutoDismiss();
                else
                    notification.StartAutoDismiss();
            });
        });
    }
    
    
    // Dismiss given notification.
    internal void DismissNotification(Notification notification)
    {
        // check state
        this.VerifyAccess();
        if (!this.notifications.Contains(notification)
            || !this.dismissingNotifications.Add(notification))
        {
            return;
        }
        
        // detach from to notification
        notification.PropertyChanged -= this.OnNotificationPropertyChanged;
        
        // start dismissing notification
        if (this.notificationControls.TryGetValue(notification, out var control))
            control.Opacity = 0;
        this.notificationActionsPanels.Remove(notification);
        this.SynchronizationContext.PostDelayed(() =>
        {
            if (!this.notifications.Remove(notification)
                || !this.dismissingNotifications.Remove(notification))
            {
                return;
            }
            if (control is not null)
            {
                this.notificationControls.Remove(notification);
                this.notificationsPanel?.Children.Remove(control);
            }
            notification.OnDismissed();
        }, (int)this.FindResourceOrDefault("TimeSpan/NotificationPresenter.Notification", TimeSpan.FromMilliseconds(500)).TotalMilliseconds);
    }


    /// <summary>
    /// Get or set horizontal alignment of notifications.
    /// </summary>
    public HorizontalAlignment HorizontalNotificationsAlignment
    {
        get => this.GetValue<HorizontalAlignment>(HorizontalNotificationsAlignmentProperty);
        set => this.SetValue(HorizontalNotificationsAlignmentProperty, value);
    }


    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.notificationControls.Clear();
        this.notificationsPanel?.Children.Clear();
        this.notificationActionControlTemplate = this.FindResource("DataTemplate/NotificationPresenter.Notification.Action") as DataTemplate;
        this.notificationControlTemplate = this.FindResource("DataTemplate/NotificationPresenter.Notification") as DataTemplate;
        this.notificationsPanel = e.NameScope.Find<Panel>("PART_NotificationsPanel")?.Also(it =>
        {
            foreach (var notification in this.notifications)
            {
                this.BuildNotificationControl(notification)?.Let(control =>
                {
                    this.notificationControls[notification] = control;
                    it.Children.Add(control);
                    control.Opacity = 1;
                    (control.RenderTransform as TranslateTransform)?.Let(it =>
                    {
                        it.X = 0;
                        it.Y = 0;
                    });
                });
            }
        });
    }


    // Called when property of notification changed.
    void OnNotificationPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Notification notification)
            return;
        if (e.Property == Notification.ActionsProperty)
        {
            if (!this.notificationActionsPanels.TryGetValue(notification, out var actionsPanel))
                return;
            actionsPanel.Children.Clear();
            this.BuildNotificationActionControls(notification, actionsPanel);
        }
    }


    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(NotificationPresenter);


    /// <inheritdoc/>
    public SynchronizationContext SynchronizationContext { get; }
    
    
    /// <summary>
    /// Get or set vertical alignment of notifications.
    /// </summary>
    public VerticalAlignment VerticalNotificationsAlignment
    {
        get => this.GetValue<VerticalAlignment>(VerticalNotificationsAlignmentProperty);
        set => this.SetValue(VerticalNotificationsAlignmentProperty, value);
    }
}