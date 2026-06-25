#!/usr/bin/env sh

DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
JAVA_CMD="${JAVA_HOME:+$JAVA_HOME/bin/}java"

exec "$JAVA_CMD" -classpath "$DIR/gradle/wrapper/gradle-wrapper.jar" org.gradle.wrapper.GradleWrapperMain "$@"
