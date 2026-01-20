using Bogus;

namespace Demo.Models.Faker;

public class ItemFaker : Faker<Item>
{
    #region public constructors

    public ItemFaker(int seed)
    {
        ConfigureItemFaker(seed);
    }

    #endregion 

    #region private methods

    private void ConfigureItemFaker(int seed)
    {
        UseSeed(seed)
            .CustomInstantiator(f => new(f.PickRandom(_itemNames), f.Random.Int(1, 20)));
    }

    private static readonly string[] _itemNames =
    [
        "Laptop",
        "Smartphone",
        "Headphones",
        "Monitor",
        "Keyboard",
        "Mouse",
        "Printer",
        "Tablet",
        "Camera",
        "Smartwatch",
        "Speaker",
        "Router",
        "External Hard Drive",
        "Webcam",
        "Microphone",
        "Charger",
        "USB Flash Drive",
        "Graphics Card",
        "Motherboard",
        "Power Supply",
        "Cooling Fan",
        "VR Headset",
        "Gaming Console",
        "E-reader",
        "Projector",
        "Smart Home Hub",
        "Fitness Tracker",
        "Drone",
        "3D Printer",
        "Action Camera",
        "Smart Light Bulb"
    ];

    #endregion 
}
