// MultiplayerClient.js - WebSocket multiplayer (optional feature)
class MultiplayerClient {
    constructor() {
        this.ws = null;
        this.connected = false;
        this.playerId = null;
        this.players = {};
        this.isHost = false;
        this.onPlayerJoined = null;
        this.onPlayerLeft = null;
        this.onPlayerUpdate = null;
        this.onGameState = null;
        this.playerMeshes = {};
        this.scene = null;
    }

    connect(address, playerName, isHost) {
        try {
            this.ws = new WebSocket(address);
            this.isHost = isHost;

            this.ws.onopen = () => {
                this.connected = true;
                this.send({
                    type: 'join',
                    name: playerName,
                    isHost: isHost
                });
                console.log('Connected to server');
            };

            this.ws.onmessage = (event) => {
                const data = JSON.parse(event.data);
                this.handleMessage(data);
            };

            this.ws.onclose = () => {
                this.connected = false;
                console.log('Disconnected from server');
                this.cleanupPlayers();
            };

            this.ws.onerror = (err) => {
                console.error('WebSocket error:', err);
                this.connected = false;
            };
        } catch (e) {
            console.error('Failed to connect:', e);
        }
    }

    disconnect() {
        if (this.ws) {
            this.ws.close();
            this.ws = null;
        }
        this.connected = false;
        this.cleanupPlayers();
    }

    send(data) {
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify(data));
        }
    }

    handleMessage(data) {
        switch (data.type) {
            case 'welcome':
                this.playerId = data.id;
                break;

            case 'player_joined':
                this.players[data.id] = {
                    name: data.name,
                    position: { x: 0, y: 0, z: 0 },
                    rotation: 0,
                    health: 100
                };
                this.createPlayerMesh(data.id, data.name);
                if (this.onPlayerJoined) this.onPlayerJoined(data);
                break;

            case 'player_left':
                this.removePlayerMesh(data.id);
                delete this.players[data.id];
                if (this.onPlayerLeft) this.onPlayerLeft(data);
                break;

            case 'player_update':
                if (this.players[data.id]) {
                    this.players[data.id].position = data.position;
                    this.players[data.id].rotation = data.rotation;
                    this.players[data.id].health = data.health;
                }
                break;

            case 'game_state':
                if (this.onGameState) this.onGameState(data);
                break;

            case 'chat':
                console.log(`[Chat] ${data.name}: ${data.message}`);
                break;
        }
    }

    createPlayerMesh(id, name) {
        if (!this.scene) return;

        const group = new THREE.Group();

        // Body
        const body = new THREE.Mesh(
            new THREE.BoxGeometry(0.5, 1.2, 0.35),
            new THREE.MeshStandardMaterial({ color: 0x2266aa })
        );
        body.position.y = 1.0;
        body.castShadow = true;
        group.add(body);

        // Head
        const head = new THREE.Mesh(
            new THREE.SphereGeometry(0.2, 8, 8),
            new THREE.MeshStandardMaterial({ color: 0xddaa88 })
        );
        head.position.y = 1.8;
        group.add(head);

        // Name tag - using a simple sprite
        const canvas = document.createElement('canvas');
        canvas.width = 256;
        canvas.height = 64;
        const ctx = canvas.getContext('2d');
        ctx.fillStyle = 'rgba(0,0,0,0.5)';
        ctx.fillRect(0, 0, 256, 64);
        ctx.fillStyle = '#ffffff';
        ctx.font = '24px Arial';
        ctx.textAlign = 'center';
        ctx.fillText(name, 128, 42);

        const texture = new THREE.CanvasTexture(canvas);
        const spriteMat = new THREE.SpriteMaterial({ map: texture, transparent: true });
        const sprite = new THREE.Sprite(spriteMat);
        sprite.position.y = 2.3;
        sprite.scale.set(2, 0.5, 1);
        group.add(sprite);

        this.scene.add(group);
        this.playerMeshes[id] = group;
    }

    removePlayerMesh(id) {
        if (this.playerMeshes[id] && this.scene) {
            this.scene.remove(this.playerMeshes[id]);
            delete this.playerMeshes[id];
        }
    }

    cleanupPlayers() {
        Object.keys(this.playerMeshes).forEach(id => {
            this.removePlayerMesh(id);
        });
        this.players = {};
    }

    sendPosition(position, rotation, health) {
        this.send({
            type: 'update',
            position: { x: position.x, y: position.y, z: position.z },
            rotation: rotation,
            health: health
        });
    }

    sendChat(message) {
        this.send({ type: 'chat', message });
    }

    update() {
        // Update other player meshes
        Object.keys(this.players).forEach(id => {
            if (id === this.playerId) return;
            const player = this.players[id];
            const mesh = this.playerMeshes[id];
            if (mesh && player.position) {
                mesh.position.lerp(
                    new THREE.Vector3(player.position.x, 0, player.position.z),
                    0.1
                );
                mesh.rotation.y = player.rotation || 0;
            }
        });
    }
}
