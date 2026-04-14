// server.js - WebSocket multiplayer server (Node.js)
const WebSocket = require('ws');

const PORT = process.env.PORT || 3000;
const wss = new WebSocket.Server({ port: PORT });

const players = new Map();
let nextId = 1;

console.log(`Zombie Survival Server running on port ${PORT}`);

wss.on('connection', (ws) => {
    const playerId = 'player_' + nextId++;
    let playerName = 'Unknown';

    ws.on('message', (raw) => {
        try {
            const data = JSON.parse(raw);

            switch (data.type) {
                case 'join':
                    playerName = data.name || 'Jogador';
                    players.set(playerId, {
                        ws, name: playerName, isHost: data.isHost,
                        position: { x: 0, y: 0, z: 0 }, rotation: 0, health: 100
                    });

                    // Welcome the new player
                    ws.send(JSON.stringify({ type: 'welcome', id: playerId }));

                    // Notify existing players
                    broadcast({ type: 'player_joined', id: playerId, name: playerName }, playerId);

                    // Send existing players to new player
                    players.forEach((p, id) => {
                        if (id !== playerId) {
                            ws.send(JSON.stringify({
                                type: 'player_joined', id, name: p.name
                            }));
                        }
                    });

                    console.log(`${playerName} (${playerId}) joined. Total: ${players.size}`);
                    break;

                case 'update':
                    const player = players.get(playerId);
                    if (player) {
                        player.position = data.position;
                        player.rotation = data.rotation;
                        player.health = data.health;
                        broadcast({
                            type: 'player_update', id: playerId,
                            position: data.position, rotation: data.rotation,
                            health: data.health
                        }, playerId);
                    }
                    break;

                case 'chat':
                    broadcast({
                        type: 'chat', id: playerId,
                        name: playerName, message: data.message
                    });
                    break;

                case 'game_state':
                    // Host broadcasts game state (zombies, pickups, etc.)
                    broadcast({ ...data, id: playerId }, playerId);
                    break;
            }
        } catch (e) {
            console.error('Message parse error:', e);
        }
    });

    ws.on('close', () => {
        players.delete(playerId);
        broadcast({ type: 'player_left', id: playerId, name: playerName });
        console.log(`${playerName} (${playerId}) left. Total: ${players.size}`);
    });

    ws.on('error', (err) => {
        console.error(`Error for ${playerId}:`, err.message);
    });
});

function broadcast(data, excludeId) {
    const msg = JSON.stringify(data);
    players.forEach((player, id) => {
        if (id !== excludeId && player.ws.readyState === WebSocket.OPEN) {
            player.ws.send(msg);
        }
    });
}
