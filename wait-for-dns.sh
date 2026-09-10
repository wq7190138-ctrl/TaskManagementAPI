#!/bin/sh
set -e

HOST=${1:-redis_master}
echo "⏳ 等待 $HOST 主机名解析..."

while true; do
    if nslookup $HOST > /dev/null 2>&1; then
        echo "✅ 主机名 $HOST 解析成功"
        break
    fi
    sleep 1
done

echo "🚀 启动哨兵..."
exec redis-sentinel /usr/local/etc/redis/sentinel.conf