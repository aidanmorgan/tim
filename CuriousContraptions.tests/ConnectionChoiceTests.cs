using Godot;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ConnectionChoiceTests
{
    [Fact]
    public void EveryElectricalSocketPairHasDistinctPresentationAndAnActualIcon()
    {
        var labels=new HashSet<string>(StringComparer.Ordinal);
        foreach(var output in new[]{SocketId.Supply,SocketId.ExtendedOut,SocketId.RetractedOut})
        foreach(var input in new[]{SocketId.PowerIn,SocketId.FirstIn,SocketId.SecondIn,SocketId.ExtendIn,SocketId.RetractIn})
        {
            var choice=ConnectionChoice.Describe(new(){Type=ConnectionDomain.Electrical,FromPort=output,ToPort=input});
            Assert.True(labels.Add(choice.Label));
            var icon=WorkshopIcons.ConnectionPictogram(choice);
            Assert.Equal(output==SocketId.Supply?48:100,icon.GetWidth());
            Assert.Equal(48,icon.GetHeight());
            using var pixels=icon.GetImage();
            Assert.True(pixels.GetUsedRect().Size.X>0);
        }
        Assert.Equal(15,labels.Count);
    }

    [Fact]
    public void InvalidElectricalPortsAreRejected()
    {
        foreach(var pair in new[]{(SocketId.Drive,SocketId.PowerIn),(SocketId.Supply,SocketId.DriveIn),
            ((SocketId)999,SocketId.ExtendIn),(SocketId.ExtendedOut,(SocketId)999)})
            Assert.Throws<InvalidOperationException>(()=>ConnectionChoice.Describe(new(){
                Type=ConnectionDomain.Electrical,FromPort=pair.Item1,ToPort=pair.Item2}));
    }
}
