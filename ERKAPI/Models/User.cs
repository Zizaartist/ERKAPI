﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace ERKAPI.Models
{
    public partial class User
    {
        public User()
        {
            BlacklistedPosts = new HashSet<BlacklistedPost>();
            Comments = new HashSet<Comment>();
            DiasporaRequests = new HashSet<DiasporaRequest>();
            Opinions = new HashSet<Opinion>();
            Posts = new HashSet<Post>();
            Reports = new HashSet<Report>();
            SubscriptionsEntities = new HashSet<Subscription>();
            SubscribersEntities = new HashSet<Subscription>();
            Subscribers = new HashSet<User>();
            Subscriptions = new HashSet<User>();
        }

        public int UserId { get; set; }
        [Required]
        [StringLength(250, MinimumLength = 2)]
        public string Name { get; set; }
        [StringLength(50, MinimumLength = 6)]
        [Phone]
        public string Phone { get; set; }
        [StringLength(250, MinimumLength = 6)]
        [EmailAddress]
        public string Email { get; set; }
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
        public bool ShowDoB { get; set; }
        public string Avatar { get; set; }
        public int SubscriptionCount { get; set; }
        public int SubscriberCount { get; set; }

        public virtual ICollection<BlacklistedPost> BlacklistedPosts { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<DiasporaRequest> DiasporaRequests { get; set; }
        public virtual ICollection<Opinion> Opinions { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
        public virtual ICollection<Report> Reports { get; set; }
        [JsonIgnore]
        public virtual ICollection<Subscription> SubscriptionsEntities { get; set; }
        [JsonIgnore]
        public virtual ICollection<Subscription> SubscribersEntities { get; set; }
        public virtual ICollection<User> Subscriptions { get; set; }
        public virtual ICollection<User> Subscribers { get; set; }

        [JsonIgnore]
        [NotMapped]
        public bool ShowSensitiveData { get; set; } = false;

        public bool ShouldSerializePhone() => ShowSensitiveData;
        public bool ShouldSerializeEmail() => ShowSensitiveData;
        public bool ShouldSerializeShowDoB() => ShowSensitiveData;
        public bool ShouldSerializeDateOfBirth() => ShowDoB;

    }
}