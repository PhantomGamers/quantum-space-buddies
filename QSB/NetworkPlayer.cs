﻿using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB {
    public class NetworkPlayer: NetworkBehaviour {
        Transform _body;
        public static NetworkPlayer localInstance { get; private set; }

        void Start () {
            QSB.Log("Start NetworkPlayer", netId.Value);
            SectorSync.SetSector(netId, Sector.Name.TimberHearth);

            transform.parent = Locator.GetRootTransform();

            var player = Locator.GetPlayerBody().transform.Find("Traveller_HEA_Player_v2");
            if (isLocalPlayer) {
                localInstance = this;
                _body = player;
            } else {
                _body = Instantiate(player);
                _body.GetComponent<PlayerAnimController>().enabled = false;
                _body.Find("player_mesh_noSuit:Traveller_HEA_Player/player_mesh_noSuit:Player_Head").gameObject.layer = 0;
                _body.Find("Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:PlayerSuit_Helmet").gameObject.layer = 0;
            }
        }

        public void EnterSector (Sector sector) {
            var name = sector.GetName();
            if (name != Sector.Name.Unnamed && name != Sector.Name.Ship && name != Sector.Name.Sun) {
                SectorSync.SetSector(netId, sector.GetName());
                SectorMessage msg = new SectorMessage();
                msg.sectorId = (int) sector.GetName();
                msg.senderId = netId.Value;
                connectionToServer.Send(MessageType.Sector, msg);
            }
        }

        void Update () {
            if (!_body) {
                return;
            }

            var sectorTransform = SectorSync.GetSector(netId);
            if (isLocalPlayer) {
                transform.position = sectorTransform.InverseTransformPoint(_body.position);
                transform.rotation = sectorTransform.InverseTransformRotation(_body.rotation);
            } else {
                _body.parent = sectorTransform;
                _body.position = sectorTransform.TransformPoint(transform.position);
                _body.rotation = sectorTransform.rotation * transform.rotation;
            }
        }
    }
}
