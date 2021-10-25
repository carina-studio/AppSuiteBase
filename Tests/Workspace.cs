using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Tests
{
    class Workspace : ViewModels.MainWindowViewModel<App>
    {
        public Workspace(JsonElement? savedState)
        {
            if (savedState != null)
            {
                var obj = savedState.Value;
                this.ID = obj.GetProperty("ID").GetInt32();
            }
            else
                this.ID = new Random().Next();
        }


        public int ID { get; }


        protected override string? OnUpdateTitle()
        {
            return "AppSuite Test";
        }


        public override void SaveState(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteNumber("ID", this.ID);
            writer.WriteEndObject();
        }
    }
}
