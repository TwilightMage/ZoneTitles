using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ZoneTitles.Common.UI;

public class Widget : UIElement
{
    public bool ClickTransparent = false;

    protected bool TestSelfClicked(UIMouseEvent evt)
    {
        UIElement element = evt.Target;

        while (element != null)
        {
            if (element == this) return true;
            if (element is Widget widget && widget.ClickTransparent || element is UIText or UIImage or UIImageFramed or UISlicedImage or UIToggleImage)
            {
                element = element.Parent;
            }
            else
            {
                break;
            }
        }

        return false;
    }
}