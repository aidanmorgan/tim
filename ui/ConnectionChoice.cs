using System;
namespace CuriousContraptions;

/// <summary>Presentation derived from typed sockets, never a label used to decide game behaviour.</summary>
public readonly record struct ConnectionChoice(string Label, string Icon, string? OutputIcon = null)
{
    public static ConnectionChoice Describe(ConnectionSpec option)
    {
        var choice=option.Type switch
        {
            ConnectionDomain.Activation=>option.ToPort switch
            {
                SocketId.SetIn=>new ConnectionChoice("Connect set","set_input"),
                SocketId.ResetIn=>new("Connect reset","reset_input"),
                SocketId.ActivationIn=>new("Connect activation","switch"),
                _=>throw new InvalidOperationException("Unsupported activation input.")
            },
            ConnectionDomain.Electrical=>option.ToPort switch
            {
                SocketId.FirstIn=>new("Connect first input","first_input"),
                SocketId.SecondIn=>new("Connect second input","second_input"),
                SocketId.PowerIn=>new("Connect electricity","battery"),
                SocketId.ExtendIn=>new("Connect extend","extend_input"),
                SocketId.RetractIn=>new("Connect retract","retract_input"),
                _=>throw new InvalidOperationException("Unsupported electrical input.")
            },
            ConnectionDomain.Mechanical=>new("Connect drive","conveyor"),
            ConnectionDomain.Rope=>new("Connect rope","rope_anchor"),
            _=>throw new InvalidOperationException("Unsupported selectable connection domain.")
        };
        if(option.Type!=ConnectionDomain.Electrical)return choice;
        return option.FromPort switch
        {
            SocketId.Supply=>choice,
            SocketId.ExtendedOut=>choice with { Label="Extended → "+choice.Label,OutputIcon="extended_output" },
            SocketId.RetractedOut=>choice with { Label="Retracted → "+choice.Label,OutputIcon="retracted_output" },
            _=>throw new InvalidOperationException("Unsupported electrical output.")
        };
    }
}
