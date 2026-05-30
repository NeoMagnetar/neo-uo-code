using System;
using Server;
using Server.Items;

public class GateProbe
{
    public static void Main()
    {
        Probe(new Point3D(1385, 1497, 10));
        Probe(new Point3D(1385, 1489, 10));
    }

    private static void Probe(Point3D p)
    {
        var eable = Map.Felucca.GetItemsInRange(p, 0);
        Console.WriteLine($"-- {p.X},{p.Y},{p.Z} --");
        foreach (Item item in eable)
        {
            var delta = item.Z - p.Z;
            if (delta >= -12 && delta <= 12)
            {
                Console.WriteLine($"{item.GetType().FullName} | ItemID=0x{item.ItemID:X} | Name={item.Name} | Hue={item.Hue} | Loc={item.Location}");
            }
        }
        eable.Free();
    }
}
