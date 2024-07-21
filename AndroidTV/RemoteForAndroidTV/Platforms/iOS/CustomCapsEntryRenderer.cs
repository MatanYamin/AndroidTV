// CustomCapsEntryHandler.cs
using Microsoft.Maui.Handlers;
using RemoteForAndroidTV;
using Microsoft.Maui.Platform;
using UIKit;

namespace RemoteForAndroidTV.iOS
{
    public partial class CustomCapsEntryHandler : EntryHandler
    {
        public CustomCapsEntryHandler() : base(Mapper)
        {
        }

        public CustomCapsEntryHandler(IPropertyMapper mapper) : base(mapper)
        {
        }

        protected override MauiTextField CreatePlatformView()
        {
            var textField = base.CreatePlatformView();
            textField.AutocapitalizationType = UITextAutocapitalizationType.AllCharacters;

            textField.EditingDidBegin += (sender, args) =>
            {
                textField.AutocapitalizationType = UITextAutocapitalizationType.AllCharacters;
            };

            return textField;
        }

        public static new IPropertyMapper<IEntry, CustomCapsEntryHandler> Mapper =
            new PropertyMapper<IEntry, CustomCapsEntryHandler>(EntryHandler.Mapper)
            {
                [nameof(IEntry.Text)] = MapText
            };

        public static void MapText(IEntryHandler handler, IEntry entry)
        {
            if (handler.PlatformView is MauiTextField textField)
            {
                textField.Text = entry.Text?.ToUpper();
            }
        }
    }
}
