<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:wbt="myWebTemplater.1.0" xmlns:sbt="mySiteTemplater.1.0" xmlns:pbt="myPageTemplater.1.0" exclude-result-prefixes="wbt sbt pbt"
	version="1.0">

	<xsl:import href="WEB_DOCUMENT_TEMPLATE.xsl"/>

	<xsl:variable name="ns-urlset">http://www.sitemaps.org/schemas/sitemap/0.9</xsl:variable>
	<xsl:template match="/">
		<xsl:choose>
			<xsl:when test="$OUT = 'XML'">
				<xsl:element name="urlset" namespace="{$ns-urlset}">
					<xsl:apply-templates select="/flx/app/page[not(@site-map-include = 'No')]">
						<xsl:sort select="@site-map-order" data-type="number" order="ascending"/>
					</xsl:apply-templates>
				</xsl:element>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text disable-output-escaping="yes">&lt;!DOCTYPE html&gt;</xsl:text>
				<html>
					<head>
						<title>Site Map</title>
					</head>
					<body>
						<h1>Site Map</h1>
						<xsl:apply-templates select="/flx/app/page[not(@site-map-include = 'No')]">
							<xsl:sort select="@site-map-order" data-type="number" order="ascending"/>
						</xsl:apply-templates>
					</body>
				</html>
			</xsl:otherwise>
		</xsl:choose>

	</xsl:template>

	<xsl:template match="page | content">
		<xsl:param name="page-heirarchy"/>
		<xsl:variable name="page-name" select="@name"/>
		<xsl:variable name="link">
			<xsl:apply-templates select="self::node()" mode="make-heirarchy-link"/>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="$OUT = 'XML'">
				<xsl:variable name="server" select="key('wbt:key_ServerVars', 'SERVER_NAME')"/>
				<xsl:element name="url" namespace="{$ns-urlset}">
					<xsl:element name="loc" namespace="{$ns-urlset}">
						<xsl:value-of select="concat('http://', $server, $link)"/>
					</xsl:element>
					<xsl:element name="changefreq" namespace="{$ns-urlset}">daily</xsl:element>
					<xsl:element name="priority" namespace="{$ns-urlset}">1.0</xsl:element>
				</xsl:element>
				<xsl:for-each select="h5">
					<xsl:element name="url" namespace="{$ns-urlset}">
						<xsl:element name="loc" namespace="{$ns-urlset}">
							<xsl:value-of select="concat('http://', $server, $link, '#', translate(translate(., $ABC, $abc), ' ', '-'))"/>
						</xsl:element>
						<xsl:element name="priority" namespace="{$ns-urlset}">0.5</xsl:element>
					</xsl:element>
				</xsl:for-each>
				<xsl:apply-templates select="content[not(@site-map-include = 'No') and not(@nav = 'false')]">
					<xsl:with-param name="page-heirarchy">
						<xsl:if test="string($page-heirarchy)">
							<xsl:value-of select="'.'"/>
						</xsl:if>
						<xsl:value-of select="$page-name"/>
					</xsl:with-param>
				</xsl:apply-templates>
			</xsl:when>
			<xsl:otherwise>
				<ul>
					<li>

						<a href="{$link}">
							<xsl:choose>
								<xsl:when test="@title">
									<xsl:value-of select="@title"/>
								</xsl:when>
								<xsl:when test="h1">
									<xsl:value-of select="h1"/>
								</xsl:when>
								<xsl:otherwise>
									<xsl:value-of select="@name"/>
								</xsl:otherwise>
							</xsl:choose>
						</a>
					</li>
					<xsl:if test="h5">
						<ul>
							<xsl:for-each select="h5">
								<li>
									<a href="{$link}#{translate(translate(.,$ABC, $abc), ' ', '-')}">
										<xsl:choose>
											<xsl:when test="@title">
												<xsl:value-of select="concat(., ' - ', @title)"/>
											</xsl:when>
											<xsl:otherwise>
												<xsl:value-of select="."/>
											</xsl:otherwise>
										</xsl:choose>
									</a>
								</li>
							</xsl:for-each>
						</ul>
					</xsl:if>
					<xsl:apply-templates select="content[not(@site-map-include = 'No') and not(@nav = 'false')]">
						<xsl:with-param name="page-heirarchy">
							<xsl:if test="string($page-heirarchy)">
								<xsl:value-of select="'.'"/>
							</xsl:if>
							<xsl:value-of select="$page-name"/>
						</xsl:with-param>
					</xsl:apply-templates>
				</ul>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="*" mode="make-heirarchy-link">
		<xsl:param name="link" select="@name"/>
		<xsl:choose>
			<xsl:when test="parent::content or parent::page">
				<xsl:apply-templates select="parent::page | parent::content" mode="make-heirarchy-link">
					<xsl:with-param name="link" select="concat(parent::*/@name, '.', $link)"/>
				</xsl:apply-templates>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="concat('/', $link)"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>
