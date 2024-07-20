namespace SampleApp;

class Badminton : IPlayedWithRackets, IIndoorSport, IYouHitAThing
{
    public decimal GetRacketMinAllowedLengthMm() => 0;
    public decimal GetRacketMaxAllowedLengthMm() => 680;
    public HitThingKind GetHitThing() => HitThingKind.Shuttlecock;
    public bool IsValidHitThingColor(ColorKind color) => color is ColorKind.Yellow or ColorKind.White;
}

class Tennis : IPlayedWithRackets, IIndoorSport, IOutdoorSport<TennisShoe>, IYouHitAThing
{
    public decimal GetRacketMinAllowedLengthMm() => 0;
    public decimal GetRacketMaxAllowedLengthMm() => 737;
    public bool GetCanBePlayedDuringWinter() => false;
    public HitThingKind GetHitThing() => HitThingKind.Ball;
    public TennisShoe GetShoe() => new TennisShoe();
    public bool IsValidHitThingColor(ColorKind color) => color is ColorKind.Green;
}

class Icehockey : IIndoorSport, IOutdoorSport<Skate>, IYouHitAThing
{
    public bool GetCanBePlayedDuringWinter() => true;
    public HitThingKind GetHitThing() => HitThingKind.Puck;
    public Skate GetShoe() => new();
    public bool IsValidHitThingColor(ColorKind color) => color is ColorKind.Black;
}

class Jogging<TShoe> : IOutdoorSport<TShoe> where TShoe : Shoe
{
    public bool GetCanBePlayedDuringWinter() => true;

    public TShoe GetShoe()
    {
        throw new NotImplementedException();
    }
}

abstract class Shoe
{
    public abstract string GetName();
}

class RunningShoe : Shoe
{
    public override string GetName() => "running shoe";
}

class TennisShoe : Shoe
{
    public override string GetName() => "tennis shoe";
}

class Skate : Shoe
{
    public override string GetName() => "skates";
}