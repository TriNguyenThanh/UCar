namespace UCar.Models.Enums;

public class ColorTransfer
{
    public static string GetColorHexCode(string colorName)
    {
        return colorName.ToLower() switch
        {
            "đỏ" => "#FF0000",
            "xanh dương" => "#0000FF",
            "xanh lá" => "#008000",
            "đen" => "#000000",
            "trắng" => "#FFFFFF",
            "vàng" => "#FFFF00",
            "bạc" => "#C0C0C0",
            "xám" => "#808080",
            "cam" => "#FFA500",
            "nâu" => "#A52A2A",
            "tím" => "#800080",
            "hồng" => "#FFC0CB",
            _ => "#FFFFFF", // Default to white if color not recognized
        };
    }
}
