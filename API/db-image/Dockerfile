FROM eclipse-temurin:21-jre-jammy
RUN mkdir -p /opt/db /data \
 && curl -fsSL https://repo1.maven.org/maven2/org/apache/derby/derby/10.17.1.0/derby-10.17.1.0.jar -o /opt/db/derby.jar \
 && curl -fsSL https://repo1.maven.org/maven2/org/apache/derby/derbynet/10.17.1.0/derbynet-10.17.1.0.jar -o /opt/db/derbynet.jar \
 && curl -fsSL https://repo1.maven.org/maven2/org/apache/derby/derbyshared/10.17.1.0/derbyshared-10.17.1.0.jar -o /opt/db/derbyshared.jar \
 && curl -fsSL https://repo1.maven.org/maven2/org/apache/derby/derbytools/10.17.1.0/derbytools-10.17.1.0.jar -o /opt/db/derbytools.jar \
 && curl -fsSL https://repo1.maven.org/maven2/org/hsqldb/hsqldb/2.7.4/hsqldb-2.7.4.jar -o /opt/db/hsqldb.jar \
 && curl -fsSL https://repo1.maven.org/maven2/com/h2database/h2/2.3.232/h2-2.3.232.jar -o /opt/db/h2.jar
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]
