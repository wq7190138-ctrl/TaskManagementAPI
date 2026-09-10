using System.Text.Json;

namespace TaskManagementAPI.Tools
{
    public class JsonDiff
    {
        public static string GetDiff(object oldObj, object newObj)
        {
            var oldJson = JsonSerializer.Serialize(oldObj);
            var newJson = JsonSerializer.Serialize(newObj);
            var oldDoc = JsonDocument.Parse(oldJson);
            var newDoc = JsonDocument.Parse(newJson);

            var changes = new List<string>();
            foreach (var prop in oldDoc.RootElement.EnumerateObject())
            {
                var propName = prop.Name;
                var oldVal = prop.Value.ToString();
                var newVal = newDoc.RootElement.GetProperty(propName).ToString();
                if (oldVal != newVal)
                    changes.Add($"{propName}: '{oldVal}' → '{newVal}'");
            }
            return string.Join("；", changes);
        }
    }
}
