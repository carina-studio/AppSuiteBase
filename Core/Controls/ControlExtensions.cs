using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Extensions for <see cref="Control"/>.
	/// </summary>
	public static class ControlExtensions
	{
		/// <summary>
		/// Find child control by name.
		/// </summary>
		/// <typeparam name="T">Type of child control.</typeparam>
		/// <param name="control">Parent control.</param>
		/// <param name="name">Name of child control.</param>
		/// <returns>Child control with specific name or null if no child control found.</returns>
		public static T? FindChildControl<T>(this IControl control, string name) where T : class, IControl
		{
			var controlType = typeof(T);
			if (control is ContentControl contentControl)
			{
				var child = contentControl.Content as IControl;
				if (child == null)
					return null;
				if (child.Name == name && controlType.IsAssignableFrom(child.GetType()))
					return (T)child;
				if (control is HeaderedContentControl headeredContentControl)
				{
					var header = headeredContentControl.Header as IControl;
					if (header != null)
					{
						if (header.Name == name && controlType.IsAssignableFrom(header.GetType()))
							return (T)header;
						var controlInHeader = header.FindChildControl<T>(name);
						if (controlInHeader != null)
							return controlInHeader;
					}
				}
				return child.FindChildControl<T>(name);
			}
			else if (control is Decorator decorator)
			{
				var child = decorator.Child;
				if (child == null)
					return null;
				if (child.Name == name && controlType.IsAssignableFrom(child.GetType()))
					return (T)child;
				return child.FindChildControl<T>(name);
			}
			else if (control is Panel panel)
			{
				foreach (var child in panel.Children)
				{
					if (child.Name == name && controlType.IsAssignableFrom(child.GetType()))
						return (T)child;
				}
				foreach (var child in panel.Children)
				{
					var result = child.FindChildControl<T>(name);
					if (result != null)
						return result;
				}
			}
			return null;
		}
	}
}
