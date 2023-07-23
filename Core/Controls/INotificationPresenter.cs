using CarinaStudio.Threading;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Presenter of <see cref="Notification"/>.
/// </summary>
public interface INotificationPresenter : IThreadDependent
{
    /// <summary>
    /// Enqueue a notification to be presented.
    /// </summary>
    /// <param name="notification">Notification.</param>
    void AddNotification(Notification notification);
}