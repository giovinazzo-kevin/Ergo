// See https://aka.ms/new-console-template for more information
public class StringBuffer
{
    private string text;

    public StringBuffer(string text)
    {
        this.text = text;
    }

    public int Length
    {
        get { return text.Length; }
    }

    public char this[int index]
    {
        get
        {
            return text[index];
        }
    }

    public string GetText(int start, int length)
    {
        return text.Substring(start, length);
    }

    public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    {
        text.CopyTo(sourceIndex, destination, destinationIndex, count);
    }
}
