// Scrollable container with entries that can be toggled on/off
public class ContainerToggle : Container
{
    private int selPos = 0;
    private Dictionary<string, bool> content = new Dictionary<string, bool>();

    // Zero the scroll num and selected index
    public override void Zero()
    {
        if (base.startAtBottom) { selPos = base.contentSize; }
        else { selPos = 0; }
        base.ZeroSrollPos();
    }
    
    // Move up
    public override bool Up()
    {
        if (selPos > 0)
        {
            selPos--;
            if (selPos < base.scrollPos) { base.ScrollUp(); }
            return true;
        }
        return false;
    }

    // Move down
    public override bool Down()
    {
        if (selPos < base.contentSize - 1)
        {
            selPos++;
            if (selPos > scrollPos + size) { base.ScrollDown(); }
            return true;
        }
        return false;
    }

    // Link the content of the container with a list
    public void LinkContent(Dictionary<string, bool> content)
    {
        this.content = content;
        base.contentSize = content.Count;
        base.SetScrollMax();
    }
    
    // Toggle the bool value for the currently selected item
    public bool Toggle()
    {
        content[content.ElementAt(selPos).Key] = !content.ElementAt(selPos).Value;
        return true;
    }
    
    // Render the content the container
    protected override void RenderContent()
    {
        for (int i = base.scrollPos; i < base.scrollPos + base.size; i++)
        {
            if (i < base.contentSize)
            {
                Console.WriteLine(" " + (i == selPos ? "<" : " ") + " [" + (content.ElementAt(i).Value == true ? "X" : "-") + "] " + (i == selPos ? ">" : " ") + " " + content.ElementAt(i).Key);
            }

            // Empty lines
            else { Console.WriteLine(); }
        }
    }
    
    // Constructor
    public ContainerToggle(
            int size, 
            bool startAtBottom = false) : base(size, startAtBottom)
    {
    }
}
