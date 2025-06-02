using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TournamentSystem.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            try
            {
                var member = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();
                if (member != null)
                {
                    var displayAttribute = member.GetCustomAttribute<DisplayAttribute>();
                    if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Name))
                    {
                        return displayAttribute.Name;
                    }
                }
                return enumValue.ToString();
            }
            catch
            {
                return enumValue.ToString();
            }
        }
    }
}