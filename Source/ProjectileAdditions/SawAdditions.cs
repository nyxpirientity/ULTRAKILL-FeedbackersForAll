using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public class SawAdditions : MonoBehaviour
    {
        public int HurtPlayerDamage { get; internal set; } = 25;
        public bool HurtPlayer { get; internal set; } = false;
        public AudioSource AudSrc { get; private set; } = null;
        public Nail Nail { get; private set; } = null;

        private void Awake()
        {
            AudSrc = GetComponent<AudioSource>();
            Nail = GetComponent<Nail>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            if (HurtPlayer && other.gameObject.layer == 2)
            {
                NewMovement.Instance.GetHurt(HurtPlayerDamage, true);
                HurtPlayer = false;
            }
        }
    }
}