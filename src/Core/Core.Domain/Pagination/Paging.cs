namespace Core.Domain.Pagination;

public record Paging
{
    private const int UpperSize = 100;
    private const int DefaultSize = 10;
    private const int DefaultNumber = 1;
    private const int Zero = 0;

    public Paging(int size = DefaultSize, int number = DefaultNumber)
    {
        Size = size switch
        {
            Zero => DefaultSize,
            > UpperSize => UpperSize,
            _ => size
        };

        Number = number is Zero
            ? DefaultNumber
            : number;
    }

    public int Size { get; }
    public int Number { get; }
}