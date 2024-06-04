<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" 
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:wbt="myWebTemplater.1.0"
    xmlns:exsl="http://exslt.org/common"
    exclude-result-prefixes="wbt exsl">

    <xsl:template name="wbt:debug-message.add">
        <xsl:param name="name"/>
        <xsl:param name="value"/>
        <xsl:param name="message-raw"/>
        
        <xsl:element name="{$name}">
            <xsl:choose>
                <xsl:when test="count($value) &gt; 0">
                    <xsl:for-each select="$value">
                        <xsl:element name="{local-name(.)}">
                            <xsl:if test="text()">
                                <xsl:value-of select="text()"/>
                            </xsl:if>
                            <xsl:if test="count(* | @*) > 0">
                                <xsl:for-each select="* | @*">
                                    <xsl:element name="{local-name(.)}">
                                        <xsl:value-of select="."/>
                                    </xsl:element>
                                </xsl:for-each>
                            </xsl:if>
                        </xsl:element>
                    </xsl:for-each>
                </xsl:when>
            </xsl:choose>
            <xsl:value-of select="$value"/>
        </xsl:element>
    </xsl:template>
    
    <xsl:template name="wbt:debug-message.write">
        <xsl:param name="message-raw"/>
        <dl class="dl-horizontal bg-light p-3">            
            <xsl:for-each select="exsl:node-set($message-raw)/*">
                <xsl:choose>
                    <xsl:when test="child::*">
                        <dt><xsl:value-of select="concat(local-name(.), ' (', count(child::*), ')')"/> </dt>
                        <dd>
                            <ul>
                                <xsl:for-each select="child::*">
                                    <li>
                                        <xsl:value-of select="concat(local-name(.), ': ')" disable-output-escaping="yes"/>
                                        <xsl:if test="text()">
                                            <xsl:value-of select="text()"/>
                                        </xsl:if>
                                        <xsl:if test="count(* | @*) > 0">
                                            <ul>
                                                <xsl:for-each select="* | @*">
                                                    <li><xsl:value-of select="concat(local-name(.), ': ', .)"/></li>                                            
                                                </xsl:for-each>                                                
                                            </ul>
                                        </xsl:if>
                                    </li>
                                </xsl:for-each>
                            </ul>
                        </dd>                       
                    </xsl:when>
                    <xsl:otherwise>
                        <dt><xsl:value-of select="local-name(.)"/></dt>
                        <dd><xsl:value-of select="."/></dd>                        
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:for-each>
        </dl>
    </xsl:template>
    
</xsl:stylesheet>