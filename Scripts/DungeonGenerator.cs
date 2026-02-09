using Godot;
using System;
using System.Collections.Generic;

public partial class DungeonGenerator : Node
{
	[Export] private TileMapLayer tileMapLayer;
	[Export] private int tileSize = 16;
	
	private Vector2I floorTile = new Vector2I(1, 1);
	
	private Vector2I rightWallTile = new Vector2I(2, 1);
	private Vector2I leftWallTile = new Vector2I(0, 1);
	private Vector2I topWallTile = new Vector2I(1, 0);
	private Vector2I bottomWallTile = new Vector2I(1, 2);
	
	private Vector2I topRightCornerTile = new Vector2I(2, 0);
	private Vector2I topLeftCornerTile = new Vector2I(0, 0);
	private Vector2I bottomRightCornerTile = new Vector2I(2, 2);
	private Vector2I bottomLeftCornerTile = new Vector2I(0, 2);
	
	private Vector2I rightTopCorridorWallTile = new Vector2I(0, 4);
	private Vector2I leftTopCorridorWallTile = new Vector2I(1, 4);
	private Vector2I rightBottomCorridorWallTile = new Vector2I(0, 3);
	private Vector2I leftBottomCorridorWallTile = new Vector2I(1, 3);
	
	private List<Room> rooms = new List<Room>();
	private List<Corridor> corridors = new List<Corridor>();
	
	private Vector2I startingPosition = new Vector2I(16, 16);
	
	[Export] private int minRoomSize = 8;
	[Export] private int maxRoomSize = 12;
	
	[Export] private int minCorridorLength = 1;
	[Export] private int maxCorridorLength = 3;
	
	//minCorridorWidth must be: minRoomSize - (minRoomSize - 2)
	//maxCorridorWidth must be: maxRoomSize - (maxRoomSize - 2)
	private int minCorridorWidth = 2;
	private int maxCorridorWidth = 3;
	
	[Export(PropertyHint.Range, "3, 100")] private int maxRoomCount = 100;
	
	[Export] private CharacterBody2D player;
	
	private RandomNumberGenerator rng = new();
	
	private int generatedRoomsCount = 0;
	
	private Dictionary<Vector2I, Vector2I> posToAtlasCoords = new Dictionary<Vector2I, Vector2I>();
	
	public override void _Ready()
	{
		rng.Seed = GD.Randi();
		
		Room startingRoom = RandomRoom(startingPosition);
		rooms.Add(startingRoom);
		
		//Spawns the player in the middle of the starting room
		player.Position = new Vector2(tileSize + (tileSize * startingRoom.W / 2), tileSize + (tileSize * startingRoom.H / 2));
		
		//Adds the data to the posToAtlasCoords dictionary
		AddRoomTileData(startingRoom);
		
		//Creates 1/3 of the rooms using the max room count
		LayoutBranch(startingRoom);
		
		//Creates the rest of the rooms and connects them to existing rooms so that there are dead-ends
		AddRoomsToExistingRooms();
		
		PlaceAllTiles();
	}
	
	private void PlaceAllTiles()
	{
		foreach(var i in posToAtlasCoords.Keys)
		{
			ChangeTile(i, posToAtlasCoords[i]);
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ReloadScene"))
		{
			GetTree().ReloadCurrentScene();
		}
	}
	
	private Room RandomRoom(Vector2I corridorEnd, Corridor corridor = null)
	{
		Room room = new Room();
		Vector2 roomSize = RandomRoomSize();
		
		room.W = (int)roomSize.X;
		room.H = (int)roomSize.Y;
		
		if (corridorEnd != startingPosition)
		{
			if (corridor.Dir == 0)
			{
				room.X = (int)(rng.RandiRange(corridorEnd.X - (room.W + tileSize), corridorEnd.X - (room.W * tileSize) + (corridor.Width + 2) * tileSize ));
				room.Y = corridorEnd.Y - (room.H * tileSize);
			}
			else if (corridor.Dir == 1)
			{
				room.X = corridorEnd.X;
				room.Y = (int)rng.RandiRange(corridorEnd.Y - (room.H + tileSize), corridorEnd.Y - (room.H - 2 - corridor.Width) * tileSize);
			}
			else if (corridor.Dir == 2)
			{
				room.X = (int)(rng.RandiRange(corridorEnd.X - (room.W + tileSize), corridorEnd.X - (room.W * tileSize) + (corridor.Width + 2) * tileSize ));
				room.Y = corridorEnd.Y;
			}
			else if (corridor.Dir == 3)
			{
				room.X = corridorEnd.X - (room.W * tileSize) + tileSize;
				room.Y = (int)rng.RandiRange(corridorEnd.Y - (room.H + tileSize), corridorEnd.Y - (room.H - 2 - corridor.Width) * tileSize);
			}
			
		}
		else
		{
			room.X = corridorEnd.X;
			room.Y = corridorEnd.Y;
		}
		
		Vector2I firstTilePosition = ToTilePosition(room.X, room.Y);
		
		room.TileX = firstTilePosition.X;
		room.TileY = firstTilePosition.Y;
		
		return room;
	}
	
	private void AddConnectedRoomTileData(Room room, Corridor corridor, Vector2I corridorEnd)
	{
		if (corridor.Dir == 0)
		{
			//Replaces the wall tiles that are next to the new floor tile with different wall tiles
			posToAtlasCoords[ToTilePosition(corridorEnd.X - tileSize, room.Y + (room.H * tileSize) - tileSize)] = leftBottomCorridorWallTile;
			posToAtlasCoords[ToTilePosition(corridorEnd.X + (tileSize * corridor.Width), room.Y + (room.H * tileSize) - tileSize)] = rightBottomCorridorWallTile;
			
			//Replaces the second room's wall tile with a floor tile
			posToAtlasCoords[ToTilePosition(corridorEnd.X, room.Y + (room.H * tileSize) - tileSize)] = floorTile;
			
			for(int w = 1; w < corridor.Width; w++)
			{
				posToAtlasCoords[ToTilePosition(corridorEnd.X + (tileSize * w), room.Y + (room.H * tileSize) - tileSize)] = floorTile;
			}
		}
		else if (corridor.Dir == 1)
		{
			for(int w = 1; w <= corridor.Width; w++)
			{
				posToAtlasCoords[ToTilePosition(corridorEnd.X, corridorEnd.Y + (tileSize * w) - tileSize)] = floorTile;
			}
			
			posToAtlasCoords[ToTilePosition(corridorEnd.X, corridorEnd.Y - tileSize)] = leftTopCorridorWallTile;
			posToAtlasCoords[ToTilePosition(corridorEnd.X, corridorEnd.Y + (tileSize * corridor.Width))] = leftBottomCorridorWallTile;
		}
		else if (corridor.Dir == 2)
		{
			for(int w = 1; w <= corridor.Width; w++)
			{
				posToAtlasCoords[ToTilePosition(corridorEnd.X + (tileSize * w) - tileSize, corridorEnd.Y)] = floorTile;
			}
			
			posToAtlasCoords[ToTilePosition(corridorEnd.X - tileSize, corridorEnd.Y)] =  leftTopCorridorWallTile;
			posToAtlasCoords[ToTilePosition(corridorEnd.X + (tileSize * corridor.Width), corridorEnd.Y)] =  rightTopCorridorWallTile;
		}
		else if (corridor.Dir == 3)
		{
			for(int w = 1; w <= corridor.Width; w++)
			{
				posToAtlasCoords[ToTilePosition(corridorEnd.X, corridorEnd.Y + (tileSize * w) - tileSize)] = floorTile;
			}
			
			posToAtlasCoords[ToTilePosition(corridorEnd.X, corridorEnd.Y - tileSize)] =  rightTopCorridorWallTile;
			posToAtlasCoords[ToTilePosition(corridorEnd.X, corridorEnd.Y + (tileSize * corridor.Width))] =  rightBottomCorridorWallTile;
		}
	}
	
	private Vector2 RandomRoomSize()
	{
		return new Vector2(rng.RandiRange(minRoomSize, maxRoomSize), rng.RandiRange(minRoomSize, maxRoomSize));
	}
	
	private Corridor RandomCorridor(Room room, int dir)
	{
		Corridor corridor = new Corridor();
		
		corridor.Length = rng.RandiRange(minCorridorLength, maxCorridorLength);
		corridor.Width = rng.RandiRange(minCorridorWidth, maxCorridorWidth);
		
		if (dir == 0)
		{
			corridor.X = (int)(rng.RandiRange(room.TileX + 2, room.TileX + (room.W - corridor.Width - 2) )) * tileSize;
			corridor.Y = room.Y;
		}
		else if (dir == 1)
		{
			corridor.X = room.X + room.W * tileSize;
			corridor.Y = (int)(rng.RandiRange(room.TileY + 2, room.TileY + (room.H - corridor.Width - 2) )) * tileSize;
		}
		else if (dir == 2)
		{
			corridor.X = (int)(rng.RandiRange(room.TileX + 2, room.TileX + (room.W - corridor.Width - 2) )) * tileSize;
			corridor.Y = room.Y + room.H * tileSize;
		}
		else if (dir == 3)
		{
			corridor.X = room.X - tileSize;
			corridor.Y = (int)(rng.RandiRange(room.TileY + 2, room.TileY + (room.H - corridor.Width - 2))) * tileSize;
		}
		
		corridor.Dir = dir;
		
		return corridor;
	}
	
	private Vector2I GetCorridorEnd(Corridor corridor)
	{
		Vector2I corridorEnd = corridor.Dir switch
		{
			0 => new Vector2I(corridor.X, corridor.Y - (tileSize * corridor.Length)),
			1 => new Vector2I(corridor.X + (tileSize * corridor.Length), corridor.Y),
			2 => new Vector2I(corridor.X, corridor.Y + (tileSize * corridor.Length)),
			_ => new Vector2I(corridor.X - (tileSize * corridor.Length), corridor.Y),
		};
		
		return corridorEnd;
	}
	
	private void AddCorridorTileData(Corridor corridor, int dir)
	{
		if (dir == 0)
		{
			//Replaces wall tile with a floor tile
			posToAtlasCoords[ToTilePosition(corridor.X, corridor.Y)] = floorTile;
			
			for (int l = 1; l <= corridor.Length; l++)
			{
				for (int w = 1; w <= corridor.Width; w++)
				{
					//Places floor tiles in an L shape
					posToAtlasCoords[ToTilePosition(corridor.X, corridor.Y - (tileSize * l))] = floorTile;
					posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * w), corridor.Y)] = floorTile;
					
					//Places the rest of the floor tiles
					posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * w), corridor.Y - (tileSize * l))] = floorTile;
					
					//Places corridor tiles and walls based on length and width
					posToAtlasCoords[ToTilePosition(corridor.X - 1, corridor.Y - (tileSize * l))] = leftWallTile;
					posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * corridor.Width), corridor.Y - (tileSize * l))] = rightWallTile;
				}
			}
			
			//Replaces the wall tiles that are next to the new floor tile with different wall tiles
			posToAtlasCoords[ToTilePosition(corridor.X - 1, corridor.Y)] = leftTopCorridorWallTile;
			posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * corridor.Width), corridor.Y)] = rightTopCorridorWallTile;
		}
		else if (dir == 1)
		{
			for (int l = 1; l <= corridor.Length; l++)
			{
				for (int w = 1; w <= corridor.Width; w++)
				{
					//Replaces wall tiles with floor tiles
					posToAtlasCoords[ToTilePosition(corridor.X - tileSize, corridor.Y + (tileSize * w - 1))] = floorTile;
					
					//Rest of floor tiles
					posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * l) - tileSize, corridor.Y + (tileSize * w - 1))] = floorTile;
					
					posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * l) - tileSize, corridor.Y - 1)] = topWallTile;
					posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * l) - tileSize, corridor.Y + (tileSize * w))] = bottomWallTile;
				}
			}
			
			//Replaces the wall tiles that are next to the new floor tile with different wall tiles
			posToAtlasCoords[ToTilePosition(corridor.X - tileSize, corridor.Y - 1)] = rightTopCorridorWallTile;
			posToAtlasCoords[ToTilePosition(corridor.X - tileSize, corridor.Y + (tileSize * corridor.Width))] = rightBottomCorridorWallTile;
		}
		else if (dir == 2)
		{
			for (int l = 1; l <= corridor.Length; l++)
			{
				for (int w = 1; w <= corridor.Width; w++)
				{
					posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * w) - tileSize, corridor.Y - tileSize)] = floorTile;
					
					posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * w) - tileSize, corridor.Y + (tileSize * l) - tileSize)] = floorTile;
					
					posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * w), corridor.Y + (tileSize * l) - tileSize)] = rightWallTile;
					posToAtlasCoords[ToTilePosition(corridor.X - tileSize, corridor.Y + (tileSize * l) - tileSize)] = leftWallTile;
				}
			}
			
			posToAtlasCoords[ToTilePosition(corridor.X - 1, corridor.Y - tileSize)] = leftBottomCorridorWallTile;
			posToAtlasCoords[ToTilePosition(corridor.X + (tileSize * corridor.Width), corridor.Y - tileSize)] = rightBottomCorridorWallTile;
		}
		else if (dir == 3)
		{
			for (int l = 1; l <= corridor.Length; l++)
			{
				for (int w = 1; w <= corridor.Width; w++)
				{
					posToAtlasCoords[ToTilePosition(corridor.X + tileSize, corridor.Y + (tileSize * w) - tileSize)] = floorTile;
					posToAtlasCoords[ToTilePosition(corridor.X - (tileSize * l) + tileSize, corridor.Y + (tileSize * w) - tileSize)] = floorTile;
					
					posToAtlasCoords[ToTilePosition(corridor.X - (tileSize * l) + tileSize, corridor.Y + (tileSize * w))] = bottomWallTile;
					posToAtlasCoords[ToTilePosition(corridor.X - (tileSize * l) + tileSize, corridor.Y - 1)] = topWallTile;
				}
			}
			
			posToAtlasCoords[ToTilePosition(corridor.X + tileSize, corridor.Y - 1)] = leftTopCorridorWallTile;
			posToAtlasCoords[ToTilePosition(corridor.X + tileSize, corridor.Y + (tileSize * corridor.Width))] = leftBottomCorridorWallTile;
		}
	}
	
	private void LayoutBranch(Room connectingRoom)
	{
		for(int i = 1; i < Mathf.FloorToInt(maxRoomCount / 3); i++)
		{
			Room room = MakeMoreRooms(connectingRoom);
			connectingRoom = room;
			
		}
		
		generatedRoomsCount += Mathf.FloorToInt(maxRoomCount / 3);
	}
	
	private Room MakeMoreRooms(Room connectingRoom)
	{
		Room room = new Room();
		Corridor corridor = new Corridor();
		
		int retries = 0;
		bool valid = false;
		
		//0: Up, 1: Right, 2: Down, 3: Left
		int dir = rng.RandiRange(0, 3);
		
		List<int> FailedDirections = new List<int>();
		
		//Tries to make a room that doesn't collide with anything
		while(!valid && retries < (maxRoomCount * 4) + 1)
		{
			//Gets a direction that hasn't been tried out
			dir = GetDir(FailedDirections);
			
			if (dir == -1 || connectingRoom == null)
			{
				return null;
			}
			
			corridor = RandomCorridor(connectingRoom, dir);
			
			Vector2I corridorEnd = GetCorridorEnd(corridor);
			
			room = RandomRoom(corridorEnd, corridor);
			
			if (!IsLayoutConflicting(room))
			{
				valid = true;
				
				AddCorridorTileData(corridor, dir);
				
				AddRoomTileData(room);
				AddConnectedRoomTileData(room, corridor, corridorEnd);
			}
			else
			{
				if (!FailedDirections.Contains(dir))
				{
					FailedDirections.Add(dir);
				}
			}
			
			retries++;
		}
		
		corridors.Add(corridor);
		rooms.Add(room);
		return room;
	}
	
	private int GetDir(List<int> failedDirections)
	{
		Godot.Collections.Array<int> directions = new Godot.Collections.Array<int>()
		{
			0, 1, 2, 3
		};
		
		directions.Shuffle();
		
		foreach(var dir in directions)
		{
			if (!failedDirections.Contains(dir))
			{
				return dir;
			}
		}
		
		return -1;
	}
	
	private bool IsLayoutConflicting(Room room)
	{
		foreach(var r in rooms)
		{
			if (RoomsColliding(r, room))
			{
				return true;
			}
		}
		
		return false;
	}
	
	private bool RoomsColliding(Room r1, Room r2)
	{
		if(r1.TileX < r2.TileX + r2.W &&
		r1.TileX + r1.W > r2.TileX &&
		r1.TileY < r2.TileY + r2.H &&
		r1.TileY + r1.H > r2.TileY)
		{
			return true;
		}
		
		return false;
	}
	
	private void AddRoomsToExistingRooms()
	{
		int secondRoomsCount = Mathf.FloorToInt(maxRoomCount / 3);
		int secondRoomsStartingIndex = rooms.Count;
		
		AddExistingRoomsLoop(secondRoomsCount, 1);
		
		generatedRoomsCount += secondRoomsCount;
		int lastRoomsCount = maxRoomCount - generatedRoomsCount;
		
		AddExistingRoomsLoop(lastRoomsCount, secondRoomsStartingIndex);
		
		GD.Print($"Rooms Generated: {rooms.Count}");
	}
	
	private void AddExistingRoomsLoop(int loopingNum, int minIndex)
	{
		for(int i = 0; i < loopingNum; i++)
		{
			int randomIndex = rng.RandiRange(minIndex, rooms.Count - 1);
			Room room = MakeMoreRooms(rooms[randomIndex]);
		}
	}
	
	private void AddRoomTileData(Room room)
	{
		Vector2I tile = Vector2I.Zero;
		
		for(int w = 0; w < room.W; w++)
		{
			for(int h = 0; h < room.H; h++)
			{
				Vector2I tilePosition = ToTilePosition(room.X + w * tileSize, room.Y + h * tileSize);
				
				tile = ReturnTile(room.W, room.H, w, h);
				
				posToAtlasCoords[tilePosition] = tile;
			}
		}
	}
	
	private Vector2I ReturnTile(int roomW, int roomH, int w, int h)
	{
		if (w == 0)
		{
			if (h == 0) return topLeftCornerTile;
			else if (h == roomH - 1) return bottomLeftCornerTile;
			
			return leftWallTile;
		}
		else if (w == roomW - 1)
		{
			if (h == 0) return topRightCornerTile;
			else if (h == roomH - 1) return bottomRightCornerTile;
			
			return rightWallTile;
		}
		else if (w > 0 && w < roomW - 1 && h == 0 || h == roomH - 1)
		{
			if (h == 0) return topWallTile;
			
			return bottomWallTile;
		}
		else
		{
			return floorTile;
		}
		
	}
	
	private void ChangeTile(Vector2I tilePosition, Vector2I tile)
	{
		tileMapLayer.SetCell(tilePosition, 0, tile, 0);
	}
	
	private Vector2I ToTilePosition(int x, int y)
	{
		return (Vector2I)tileMapLayer.LocalToMap(new Vector2I(x, y));
	}
}
