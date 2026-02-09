using Godot;
using System;

public partial class Corridor : Node
{
	public int X { get; set; }
	public int Y { get; set; }
	public int W { get; set; }
	public int H { get; set; }
	
	public int Length { get; set; }
	public int Width { get; set; }
	public int Dir { get; set; }
}
