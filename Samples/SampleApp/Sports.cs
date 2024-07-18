namespace SampleApp;

class Badminton : IPlayedWithRackets, IIndoorSport, IYouHitAThing
{
    public decimal GetRacketMinAllowedLengthMm() => 0;
    public decimal GetRacketMaxAllowedLengthMm() => 680;
    public HitThingKind GetHitThing() => HitThingKind.Shuttlecock;
    public bool IsValidHitThingColor(ColorKind color) => color is ColorKind.Yellow or ColorKind.White;
}

class Tennis : IPlayedWithRackets, IIndoorSport, IOutdoorSport, IYouHitAThing
{
    public decimal GetRacketMinAllowedLengthMm() => 0;
    public decimal GetRacketMaxAllowedLengthMm() => 737;
    public bool GetCanBePlayedDuringWinter() => false;
    public HitThingKind GetHitThing() => HitThingKind.Ball;
    public bool IsValidHitThingColor(ColorKind color) => color is ColorKind.Green;
}

class Icehockey : IIndoorSport, IOutdoorSport, IYouHitAThing
{
    public bool GetCanBePlayedDuringWinter() => true;
    public HitThingKind GetHitThing() => HitThingKind.Puck;
    public bool IsValidHitThingColor(ColorKind color) => color is ColorKind.Black;
}

class Jogging : IOutdoorSport
{
    public bool GetCanBePlayedDuringWinter() => true;
}