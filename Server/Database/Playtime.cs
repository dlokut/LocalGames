﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Database
{
    [PrimaryKey(nameof(UserId), nameof(GameId))]
    public class Playtime
    {
        [ForeignKey(nameof(User))]
        public string UserId { get; set; }

        [ForeignKey(nameof(Game))]
        public Guid GameId { get; set; }

        public Game Game { get; set; }

        public User User { get; set; }

        public int PlaytimeMins { get; set; }
    }
}
